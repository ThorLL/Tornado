using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SmartSettings
{
    internal class PropCache
    {
        readonly Dictionary<string, FieldInfo> _fieldsInfoMap = new();
        readonly Dictionary<string, object> _fieldsCache = new();
        readonly Dictionary<string, Delegate> _subscribers = new();
        readonly object _obj;
        readonly Type _type;

        public PropCache(object obj)
        {
            _obj = obj;
            _type = obj.GetType();

            FieldInfo[] fields = _type.GetFields(FieldBinding.Bindings);
            
            foreach (FieldInfo field in fields)
            {
                _fieldsInfoMap[field.Name] = field;
                _fieldsCache[field.Name] = field.GetValue(_obj);
            }
        }
        
        public void LoadCache(object obj)
        {
            if (obj == null) return;

            Type objType = obj.GetType();
            if (objType != _type)
            {
                Debug.LogError($"Cannot load cached field from {nameof(_type)} into {nameof(objType)}");
                return;
            }

            foreach ((string fieldName, object cachedValue) in _fieldsCache)
            {
                _fieldsInfoMap[fieldName].SetValue(obj, cachedValue);
            }
        }
        
        public void SubscribeToValue(Action onChange, params string[] fieldNames)
        {
            foreach (string fieldName in fieldNames) SubscribeToValue<object>(_ => onChange(), fieldName);
        }

        public void SubscribeToValue<T>(Action<T> onChange, params string[] fieldNames)
        {
            foreach (string fieldName in fieldNames)
            {
                if (!_fieldsCache.ContainsKey(fieldName))
                {
                    Debug.LogError($"No field '{fieldName}' on in {nameof(_obj)}");
                    continue;
                }
                if (!_subscribers.TryAdd(fieldName, onChange)) _subscribers[fieldName] = Delegate.Combine(_subscribers[fieldName], onChange);
            }
        }

        public void CheckForChanges()
        {
            foreach ((string fieldName, FieldInfo field) in _fieldsInfoMap)
            {
                if (!_subscribers.ContainsKey(fieldName)) continue;

                object oldValue = _fieldsCache[fieldName];
                object newValue = field.GetValue(_obj);

                if (oldValue == newValue) continue;
                
                _fieldsCache[field.Name] = newValue;
                
                try { _subscribers[fieldName].DynamicInvoke(newValue); }
                catch (Exception ex) { Debug.LogError($"Error invoking change subscriber for {field.Name}: {ex.Message}"); }
            }
        }
    }
}