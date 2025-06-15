using System;

namespace SmartSettings
{
    [Serializable]
    public abstract class Settings
    {
        PropCache _init;
        PropCache _cache;
        public void Init()
        {
            _init = new PropCache(this);
            _cache = new PropCache(this);
            Prepare();
        }

        public void Drop()
        {
            _cache = null;
            _init = null;
        }

        public void Reset()
        {
            _init.LoadCache(this);
            InvokeChange();
        }

        public virtual void Prepare() { }
        protected virtual void PropertyChanged() { }

        public void InvokeChange()
        {
            PropertyChanged();
            OnChange.Invoke();
            _cache?.CheckForChanges();
        }
        
        public event Action OnChange = delegate { };
        public void SubscribeToValue(Action onChange, params string[] propertyNames) => _cache.SubscribeToValue(onChange, propertyNames);
        public void SubscribeToValue<T>(Action<T> onChange, params string[] propertyNames) => _cache.SubscribeToValue(onChange, propertyNames);
    }
}