using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System;

namespace Cysharp.Threading.Tasks.Pool
{

    public class ObjectPool<T> where T : MonoBehaviour
    {
        private readonly Transform _parentTransform;
        private readonly T _prefab;
        private bool _isWoldPosition;

        private Stack<T> _pool;

        private Action<T> _OnBeforeRentAction;
        private Action<T> _OnBeforeReturnAction;

        /// <summary>
        /// Limit of instace count.
        /// </summary>
        private int MaxPoolCount
        {
            get
            {
                return int.MaxValue;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"Pool instance parent Transform></param>
        /// <param name="prefab">Pool prefab</param>
        /// <param name="isWoldPosition">When you assign a parent Object, pass true to position the new object directly in world space. Pass false to set the Object’s position relative to its new parent.</param>
        /// <param name="beforeRentAction">Get instance from pool Action.</param>
        /// <param name="beforeReturnAction">Return instance to pool Action.</param>
        public ObjectPool(Transform parent, T prefab, bool isWoldPosition, Action<T> beforeRentAction = null, Action<T> beforeReturnAction = null)
        {
            _parentTransform = parent;
            _prefab = prefab;
            _isWoldPosition = isWoldPosition;
            _OnBeforeRentAction = beforeRentAction;
            _OnBeforeReturnAction = beforeReturnAction;
        }

        /// <summary>
        /// Fill pool before rent operation.
        /// </summary>
        /// <param name="preloadCount">Pool instance count.</param>
        /// <param name="threshold">Create count per frame.</param>
        public async UniTask PreLoadAsync(uint preloadCount, uint threshold)
        {
            if (_pool == null)
            {
                _pool = new Stack<T>((int)preloadCount);
            }

            int thresholdCount = 0;
            for (int i = 0; i < preloadCount; i++)
            {
                var instance = CreateInstance();

                Return(instance);

                thresholdCount++;

                if (thresholdCount == threshold)
                {
                    thresholdCount = 0;
                    await UniTask.Yield();
                }
            }
        }

        private T CreateInstance()
        {
            var obj = GameObject.Instantiate(_prefab, _parentTransform, _isWoldPosition);
            return obj;
        }

        /// <summary>
        /// Get instance from pool.
        /// </summary>
        public T Rent()
        {
            if (_pool == null)
            {
                _pool = new Stack<T>();
            }


            if (_pool.TryPop(out var instance) == false)
            {
                instance = CreateInstance();
            }

            OnBeforeRent(instance);

            return instance;
        }

        /// <summary>
        /// Called before return to pool, useful for set active object(it is default behavior).
        /// </summary>
        private void OnBeforeRent(T instance)
        {
            _OnBeforeRentAction?.Invoke(instance);
            instance.gameObject.SetActive(true);
        }

        /// <summary>
        /// Return instance to pool.
        /// </summary>
        public void Return(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            if (_pool == null)
            {
                _pool = new Stack<T>();
            }

            if ((_pool.Count + 1) == MaxPoolCount)
            {
                throw new InvalidOperationException("Reached Max PoolSize");
            }

            OnBeforeReturn(instance);
            _pool.Push(instance);
        }

        /// <summary>
        /// Called before return to pool, useful for set inactive object(it is default behavior).
        /// </summary>
        private void OnBeforeReturn(T instance)
        {
            _OnBeforeReturnAction?.Invoke(instance);
            instance.gameObject.SetActive(false);
        }

        /// <summary>
        /// Clear pool.
        /// </summary>
        public void Clear(bool callOnBeforeRent = false)
        {
            if (_pool == null)
            {
                return;
            }

            ClearAsMuchAsCount(0, callOnBeforeRent);
        }

        /// <summary>
        /// Called when clear useful for destroy instance or other finalize method.
        /// </summary>
        private void OnClear(T instance)
        {
            if (instance == null)
            {
                return;
            }

            var go = instance.gameObject;

            if (go == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(go);
        }

        private void ClearAsMuchAsCount(uint clearCount, bool callOnBeforeRent)
        {
            while (_pool.Count > clearCount)
            {
                var instance = _pool.Pop();
                if (callOnBeforeRent)
                {
                    OnBeforeRent(instance);
                }
                OnClear(instance);
            }
        }

        /// <summary>
        /// Trim pool instances. 
        /// </summary>
        /// <param name="instanceCountRatio">0.0f = clear all ~ 1.0f = live all.</param>
        /// <param name="minSize">Min pool count.</param>
        /// <param name="callOnBeforeRent">If true, call OnBeforeRent before OnClear.</param>
        public void Shrink(float instanceCountRatio, int minSize, bool callOnBeforeRent = false)
        {
            if (_pool == null)
            {
                return;
            }

            if (instanceCountRatio <= 0)
            {
                instanceCountRatio = 0;
            }

            if (instanceCountRatio >= 1.0f)
            {
                instanceCountRatio = 1.0f;
            }

            var size = (int)(_pool.Count * instanceCountRatio);
            size = Math.Max(minSize, size);

            ClearAsMuchAsCount((uint)size, callOnBeforeRent);
        }

        /// <summary>
        /// If needs shrink pool frequently, start check timer.
        /// </summary>
        /// <param name="checkInterval">Interval of call Shrink.</param>
        /// <param name="instanceCountRatio">0.0f = clearAll ~ 1.0f = live all.</param>
        /// <param name="minSize">Min pool count.</param>
        /// <param name="callOnBeforeRent">If true, call OnBeforeRent before OnClear.</param>
        public async UniTaskVoid StartShrinkTimer(TimeSpan checkInterval, float instanceCountRatio, int minSize, CancellationToken cancellationToken, bool callOnBeforeRent = false)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    await UniTask.Delay(checkInterval, cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                    if (ex.CancellationToken == cancellationToken)
                    {
                        break;
                    }

                    throw ex;
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                Shrink(instanceCountRatio, minSize, callOnBeforeRent);
            }
        }

    }
}