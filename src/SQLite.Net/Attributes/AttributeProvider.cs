using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace SQLite.Net.Attributes
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAttributeProvider : IFluentAttributeFactory
    {
        IEnumerable<T> GetCustomAttributes<T>(Type type) where T : Attribute;
        IEnumerable<T> GetCustomAttributes<T>(MemberInfo member) where T : Attribute;
    }

    public interface IFluentAttributeFactory
    {
        IFluentAttributeFactoryTo Bind<T>(Expression<Func<T, object>> expr);
        IAttributeProvider Bind<T, U>(params object[] args) where U : Attribute;
        IAttributeProvider BindName<T, U>(string name, params object[] args) where U : Attribute;
    }

    public interface IFluentAttributeFactoryTo
    {
        IAttributeProvider To(params Attribute[] attrs);
    }

    /// <summary>
    /// 
    /// </summary>
    public class AttributeProvider : IAttributeProvider, IFluentAttributeFactory, IFluentAttributeFactoryTo
    {
        private MemberInfo current = null;
        private Dictionary<Type, IEnumerable<Attribute>> typeBinds = new Dictionary<Type, IEnumerable<Attribute>>();
        private Dictionary<MemberInfo, IEnumerable<Attribute>> memberBinds = new Dictionary<MemberInfo, IEnumerable<Attribute>>();
        private Dictionary<Type, KeyValuePair<string, IEnumerable<Attribute>>> nameBinds = new Dictionary<Type, KeyValuePair<string, IEnumerable<Attribute>>>();

        #region IAttributeProvider Members

        public IEnumerable<T> GetCustomAttributes<T>(Type type) where T : Attribute
        {
            var lst = new List<T>();

            if (this.typeBinds.ContainsKey(type))
                lst.AddRange(this.typeBinds[type].Where(a => typeof(T) == a.GetType()).Cast<T>());

            return lst;
        }
        public IEnumerable<T> GetCustomAttributes<T>(MemberInfo member) where T : Attribute
        {
            IEnumerable<T> lst = new List<T>();

            if (this.memberBinds.ContainsKey(member))
                lst = lst.Concat(this.memberBinds[member].Where(a => typeof(T) == a.GetType()).Cast<T>());

            var type = ((PropertyInfo)member).PropertyType; //TODO: Handler this hardcode...
            if (this.typeBinds.ContainsKey(type))
                lst = lst.Concat(this.typeBinds[type].Where(a => typeof(T) == a.GetType()).Cast<T>());

            KeyValuePair<string, IEnumerable<Attribute>> pair;
            if (this.nameBinds.TryGetValue(type, out pair) && pair.Key == member.Name)
                lst = lst.Concat(pair.Value.Where(a => typeof(T) == a.GetType())).Cast<T>();

            return lst;
        }

        #endregion

        IAttributeProvider IFluentAttributeFactoryTo.To(params Attribute[] attrs)
        {
            this.memberBinds.Add(this.current, this.current.GetCustomAttributes<Attribute>().Concat(attrs));
            this.current = null;

            return this;
        }

        IFluentAttributeFactoryTo IFluentAttributeFactory.Bind<T>(Expression<Func<T, object>> expr)
        {
            var expression = (MemberExpression)expr.Body;
            this.current = expression.Member;

            return this;
        }

        IAttributeProvider IFluentAttributeFactory.Bind<T, U>(params object[] args)
        {
            this.typeBinds.Add(typeof(T), new Attribute[] { Activator.CreateInstance(typeof(U), args) as Attribute });

            return this;
        }

        IAttributeProvider IFluentAttributeFactory.BindName<T, U>(string name, params object[] args)
        {
            var attrs = new Attribute[] { Activator.CreateInstance(typeof(U), args) as Attribute };

            this.nameBinds.Add(typeof(T), new KeyValuePair<string, IEnumerable<Attribute>>(name, attrs));

            //if (!this.typeBinds.ContainsKey(typeof(T)))
            //    this.typeBinds.Add(typeof(T), attrs);
            //else
            //    this.typeBinds[typeof(T)] = this.typeBinds[typeof(T)].Concat(attrs);

            return this;
        }

        public static IFluentAttributeFactory NewProvider()
        {
            return new AttributeProvider();
        }
    }
}
