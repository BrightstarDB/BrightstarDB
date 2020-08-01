using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using BrightstarDB.Client;

namespace BrightstarDB.Dynamic
{
    /// <summary>
    /// A Brightstar Dynamic Object.
    /// </summary>
    public class BrightstarDynamicObject : DynamicObject
    {
        private readonly IDataObject _dataObject;

        internal BrightstarDynamicObject(IDataObject dataObject)
        {
            _dataObject = dataObject;
        }

        internal IDataObject DataObject
        {
            get { return _dataObject; }
        }

        /// <summary>
        /// Dynamic method to locate the specific properties.
        /// </summary>
        /// <param name="binder">The member binder</param>
        /// <param name="result">The out result</param>
        /// <returns>Returns true if located a value</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            // if we don't have a predicate that matches then we return null and true;
            var val = _dataObject.GetPropertyValues(GetPredicateUri(binder.Name));
            result = new DynamicCollection(val.Select(x => x is IDataObject ? new BrightstarDynamicObject(x as IDataObject) : x));
            return true;
        }

        /// <summary>
        /// Sets a property based on the binding and value
        /// </summary>
        /// <param name="binder">Member binder</param>
        /// <param name="value">Value</param>
        /// <returns>True if the value is set</returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return TrySetMember(binder.Name, value);
        }

        private bool TrySetMember(string propertyName, object value)
        {
            if (value is BrightstarDynamicObject)
            {
                var val = value as BrightstarDynamicObject;
                _dataObject.SetProperty(GetPredicateUri(propertyName), val.DataObject);
            }
            else if (value is IEnumerable<object>)
            {
                var predicate = GetPredicateUri(propertyName);
                _dataObject.RemovePropertiesOfType(predicate);
                var val = value as IEnumerable<object>;
                foreach (var o in val)
                {
                    if (o is BrightstarDynamicObject)
                    {
                        var brightstarDynamicObject = o as BrightstarDynamicObject;
                        _dataObject.AddProperty(predicate, brightstarDynamicObject.DataObject);
                    }
                    else
                    {
                        // assume it is a literal                
                        _dataObject.AddProperty(predicate, o);
                    }
                }
            }
            else
            {
                var predicate = GetPredicateUri(propertyName);
                if (value is BrightstarDynamicObject)
                {
                    _dataObject.SetProperty(predicate, (value as BrightstarDynamicObject).DataObject);
                }
                else
                {
                    _dataObject.SetProperty(predicate, value);
                }
            }
            return true;
        }

        private static string GetPredicateUri(string propName)
        {
            if (propName.Contains("__"))
            {
                return propName.Replace("__", ":");
            }
            else
            {
                return Constants.GeneratedUriPrefix + propName;
            }
        }

        ///<summary>
        /// Gets or sets the property with the specified name
        ///</summary>
        ///<param name="propertyName">Name of the property</param>
        public object this[string propertyName]
        {
            get
            {
                var val = _dataObject.GetPropertyValues(GetPredicateUri(propertyName));
                return new DynamicCollection(val.Select(x => x is IDataObject ? new BrightstarDynamicObject(x as IDataObject) : x));
            }

            set { TrySetMember(propertyName, value); }
        }

        /// <summary>
        /// Returns the identity of the underlying data object
        /// </summary>
        public string Identity
        {
            get { return _dataObject.Identity; }
        }
    }
}
