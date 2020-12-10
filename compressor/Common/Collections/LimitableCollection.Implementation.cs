using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Common.Collections
{
    class LimitableCollection<T, TCollection> : LimitableCollection<T>, IDisposable
        where TCollection: IProducerConsumerCollection<T>, new()
    {
        public LimitableCollection(int maxCapacity)
        {
            if(maxCapacity == 0)
            {
                throw new ArgumentException("Can't limit collection to 0 items", "maxCapacity");
            }

            this.ConcurrentCollection = new TCollection();
            // consumers locking
            this.ConsumersCancellationTokenSource = new CancellationTokenSource();
            this.ConsumersSemaphore = new Semaphore(0, Int32.MaxValue);
            // producers locking
            this.ProducersCancellationTokenSource = new CancellationTokenSource();
            if(maxCapacity < 0)
            {
                this.MaxCapacity = -1;
                this.ProducersSemaphore = new Semaphore(Int32.MaxValue, Int32.MaxValue);
            }
            else
            {
                this.MaxCapacity = maxCapacity;
                this.ProducersSemaphore = new Semaphore(maxCapacity, maxCapacity);
            }
        }

        readonly IProducerConsumerCollection<T> ConcurrentCollection;

        // amount of threads currently trying to add to collection
        volatile int CurrentAddersCount = 0;
        // mask to indicate that adding to collection is completed
        readonly int CurrentAddersCountAddingCompletedMask = unchecked((int)0x80000000);

        readonly Semaphore ConsumersSemaphore;
        readonly CancellationTokenSource ConsumersCancellationTokenSource;
        readonly Semaphore ProducersSemaphore;
        readonly CancellationTokenSource ProducersCancellationTokenSource;

        bool IsDisposed = false;
        
        void ThrowIfDisposed()
        {
            if(IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().AssemblyQualifiedName);
            }
        }
        
        public void Dispose()
        {
            ThrowIfDisposed();
            try
            {
                ConsumersSemaphore.Dispose();
                ConsumersCancellationTokenSource.Dispose();
                ProducersSemaphore.Dispose();
                ProducersCancellationTokenSource.Dispose();
            }
            finally
            {
                IsDisposed = true;
            }
        }

        public int MaxCapacity { get; private set; }

        public int Count
        {
            get
            {
                ThrowIfDisposed();
                return ConcurrentCollection.Count;
            }
        }

        public bool IsAddingCompleted
        {
            get
            {
                ThrowIfDisposed();
                return CurrentAddersCount == CurrentAddersCountAddingCompletedMask;
            }
        }

        public bool IsCompleted
        {
            get
            {
                ThrowIfDisposed();
                return IsAddingCompleted && Count == 0;
            }
        }
        
        protected bool TryAddToCollection(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // try getting semaphore with no wait if possible or go for full waiting
            var waitSucceeded = ProducersSemaphore.WaitOne(0);
            if(!waitSucceeded && millisecondsTimeout != 0)
            {
                var waitResult = WaitHandle.WaitAny(new [] { ProducersSemaphore, cancellationToken.WaitHandle, ProducersCancellationTokenSource.Token.WaitHandle }, millisecondsTimeout);
                if(waitResult == WaitHandle.WaitTimeout)
                {
                    return false;
                }
                else
                {
                    switch(waitResult)
                    {
                        case 0:
                            waitSucceeded = true;
                            break;
                        case 1:
                            // canceled through external cancellation token
                            throw new OperationCanceledException(cancellationToken);
                        case 2:
                        default:
                            // canceled through internal cancellation token (due to concurrent with CompleteAdding)
                            throw new InvalidOperationException("Can't add to collection for which adding was completed");
                    }
                }
            }

            if(waitSucceeded)
            {
                try
                {
                    // either adding to collection would be completed
                    // or this TryAdd will actually try adding to collection
                    var awaiter = new SpinWait();
                    while(true)
                    {
                        var observedCurrentAddersCount = CurrentAddersCount;
                        // if adding was completed
                        if((observedCurrentAddersCount & CurrentAddersCountAddingCompletedMask) != 0)
                        {
                            // ... wait till all the adders completed
                            awaiter.Reset();
                            while((CurrentAddersCount & ~CurrentAddersCount) != 0)
                            {
                                awaiter.SpinOnce();
                            }
                            // and throw InvalidOpeationException due to concurrent TryAdd and CompleteAdding are not supoorted
                            throw new InvalidOperationException("Can't add to collection for which adding was completed");
                        }
                        else
                        {
                            // if this TryAdd is actually trying adding to collection
                            if(Interlocked.CompareExchange(ref CurrentAddersCount, observedCurrentAddersCount + 1, observedCurrentAddersCount) == observedCurrentAddersCount)
                            {
                                try
                                {
                                    if(observedCurrentAddersCount + 1 == ~CurrentAddersCountAddingCompletedMask)
                                    {
                                        throw new NotSupportedException("Concurrent adders amount exceeded");
                                    }
                                    if(cancellationToken.IsCancellationRequested)
                                    {
                                        throw new OperationCanceledException(cancellationToken);
                                    }
                                    
                                    if(ConcurrentCollection.TryAdd(item))
                                    {
                                        ConsumersSemaphore.Release();
                                        return true;
                                    }
                                    else
                                    {
                                        throw new InvalidOperationException("Failed to add to underlaying collection");
                                    }
                                }
                                finally
                                {
                                    Interlocked.Decrement(ref CurrentAddersCount);
                                }
                            }
                        }
                        awaiter.SpinOnce();
                    }
                }
                catch(Exception)
                {
                    ProducersSemaphore.Release();
                    throw;
                }
            }
            else
            {
                return false;
            }
        }
        public bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if(cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if(IsAddingCompleted)
            {
                throw new InvalidOperationException("Can't add to collection for which adding was completed");
            }

            return TryAddToCollection(item, millisecondsTimeout, cancellationToken);
        }

        public void CompleteAdding()
        {
            ThrowIfDisposed();

            if(IsAddingCompleted)
            {
                return;
            }

            var awaiter = new SpinWait();
            while(true)
            {
                var observedCurrentAddersCount = CurrentAddersCount;
                // if concurrent CompleteAdding is in progress waiting for adders to finish ...
                if((observedCurrentAddersCount & CurrentAddersCountAddingCompletedMask) == CurrentAddersCountAddingCompletedMask)
                {
                    // ... then wait all adders are finished
                    awaiter.Reset();
                    while((CurrentAddersCount & ~CurrentAddersCountAddingCompletedMask) != 0)
                    {
                        awaiter.SpinOnce();
                    }
                    
                    return;
                }
                else
                {
                    // ... if this CompleteAdding actually completed adding
                    if(Interlocked.CompareExchange(ref CurrentAddersCount, CurrentAddersCount | CurrentAddersCountAddingCompletedMask, observedCurrentAddersCount) == observedCurrentAddersCount)
                    {
                        // ... ... then spin util all adders have finished
                        awaiter.Reset();
                        while((CurrentAddersCount & ~CurrentAddersCountAddingCompletedMask) != 0)
                        {
                            awaiter.SpinOnce();
                        }

                        // wake up takers, if nothing to take from collection
                        if(Count == 0)
                        {
                            ConsumersCancellationTokenSource.Cancel();
                        }
                        // wake up adders
                        ProducersCancellationTokenSource.Cancel();
                        
                        return;
                    }
                }
                awaiter.SpinOnce();
            }
        }

        public bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            item = default(T);
            ThrowIfDisposed();

            if(cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            if(IsCompleted)
            {
                //throw new InvalidOperationException("Can't take out of empty collection for which adding is completed");
                return false;
            }

            // try getting semaphore with no wait if possible or go for full waiting
            var waitSucceeded = ConsumersSemaphore.WaitOne(0);
            if(!waitSucceeded && millisecondsTimeout != 0)
            {
                var waitResult = WaitHandle.WaitAny(new [] { ConsumersSemaphore, cancellationToken.WaitHandle, ConsumersCancellationTokenSource.Token.WaitHandle }, millisecondsTimeout);
                if(waitResult == WaitHandle.WaitTimeout)
                {
                    return false;
                }
                else
                {
                    switch(waitResult)
                    {
                        case 0:
                            waitSucceeded = true;
                            break;
                        case 1:
                            // canceled through external cancellation token
                            throw new OperationCanceledException(cancellationToken);
                        case 2:
                        default:
                            // canceled through internal cancellation token due to CompleteAdding
                            return false;
                    }
                }
            }

            if(waitSucceeded)
            {
                var takeSucceeded = false;
                var takeFailed = true;
                try
                {
                    if(cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }

                    takeSucceeded = ConcurrentCollection.TryTake(out item);
                    takeFailed = false;
                    if(!takeSucceeded)
                    {
                        throw new InvalidOperationException("Failed to take out of underlaying collection");
                    }
                    else
                    {
                        return true;
                    }
                }
                finally
                {
                    if(takeSucceeded)
                    {
                        ProducersSemaphore.Release();
                    }
                    else if(takeFailed)
                    {
                        ConsumersSemaphore.Release();
                    }

                    if (IsCompleted)
                    {
                        ConsumersCancellationTokenSource.Cancel();
                    }
                }
            }
            else
            {
                return false;
            }
        }
    }
}