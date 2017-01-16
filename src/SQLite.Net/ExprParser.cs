using System;
using System.Xml;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SQLite.Net
{
    /// <summary>
    /// 
    /// </summary>
    public class SQLiteExprParser
    {
        private IDictionary<string, object> args;
        private IList<Type> parameters = new List<Type>();
        private IDictionary<string, Func<string, string, string>> methods = new Dictionary<string, Func<string, string, string>>();

        #region Private constructors

        private SQLiteExprParser(ReadOnlyCollection<ParameterExpression> parameters, IDictionary<string, object> args)
        {
            this.args = args;

            foreach (ParameterExpression parameter in parameters)
                this.parameters.Add(parameter.Type);

            //Os valores strings já chegam com aspas simples.. 
            this.methods.Add("Eq", (member, arg) => string.Format("({0} = {1})", member, arg));
            this.methods.Add("Ne", (member, arg) => string.Format("({0} <> {1})", member, arg));
            this.methods.Add("Gt", (member, arg) => string.Format("({0} > {1})", member, arg));
            this.methods.Add("Ge", (member, arg) => string.Format("({0} >= {1})", member, arg));
            this.methods.Add("Lt", (member, arg) => string.Format("({0} < {1})", member, arg));
            this.methods.Add("Le", (member, arg) => string.Format("({0} <= {1})", member, arg));
        }

        #endregion

        #region Private members

        private string ProcessMethodCallExpression(MethodCallExpression expr)
        {
            //Feito para ler requisiçõs aos extensions methods eq, ne, gt, ge, lt e le...
            if (expr.Type == typeof(bool) && expr.Arguments.Count == 2)
            {
                string method = expr.Method.Name;
                string argument = this.Process(expr.Arguments[1]).ToString();
                string member = this.ProcessMemberExpression(expr.Arguments[0] as MemberExpression) as string;

                if (this.methods.ContainsKey(method))
                {
                    var namedArg = this.AdjustNamedArg(string.Format(":{0}", member));
                    this.args.Add(namedArg, argument);

                    return this.methods[method](member, namedArg);
                }
                else
                    throw new Exception(string.Format("Method '{0}' not registered.", method));
            }
            else
                throw new NotSupportedException();
        }

        private string ProcessBinaryExpression(BinaryExpression expr)
        {
            string namedArg = string.Empty;
            string propertyName = string.Empty;

            var columns = Columns.MultiColumns(expr.Left);

            switch (expr.NodeType)
            {
                case ExpressionType.AndAlso:
                    return string.Format("({0} and {1})", this.Process(expr.Left), this.Process(expr.Right));
                case ExpressionType.OrElse:
                    return string.Format("({0} or {1})", this.Process(expr.Left), this.Process(expr.Right));
                case ExpressionType.Equal:
                    //TODO: Depois refatorar isso para ficar geral...
                    if (expr.Left is UnaryExpression && ((UnaryExpression)expr.Left).Operand.Type.GetTypeInfo().IsEnum && expr.Right is ConstantExpression)
                    {
                        propertyName = this.Process(expr.Left).ToString();
                        namedArg = this.AdjustNamedArg(string.Format(":{0}", propertyName));
                        this.args.Add(namedArg, this.ConvertRightExpressionValue(((UnaryExpression)expr.Left).Operand.Type, ((ConstantExpression)expr.Right).Value));

                        return string.Format("({0} = {1})", propertyName, namedArg);
                    }
                    else if (columns.Count() > 0)
                    {
                        var statment = string.Empty;

                        var evaluate = Expression.Lambda(expr.Right).Compile();
                        object right = evaluate.DynamicInvoke();
                        //var right = this.Process(expr.Right); //CompileExpr(expr.Right, queryArgs);

                        foreach (var column in columns)
                        {
                            var value = right.GetType().GetRuntimeProperty(column.SubPropertyName).GetValue(right);

                            //statment += string.Format("({0} = {1}) and ", column.Name, column.ColumnType == typeof(string) ? "\"" + value + "\"" : value);
                            namedArg = string.Format(":{0}", column.Name);
                            statment += string.Format("({0} = {1}) and ", column.Name, namedArg);
                            this.args.Add(namedArg, value);
                        }

                        return "(" + statment.Substring(0, statment.Length - 5) + ")";
                    }
                    else
                    {
                        propertyName = this.Process(expr.Left).ToString();
                        namedArg = this.AdjustNamedArg(string.Format(":{0}", propertyName));

                        this.args.Add(namedArg, this.Process(expr.Right));

                        return string.Format("({0} = {1})", propertyName, namedArg);
                    }

                case ExpressionType.NotEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    {
                        propertyName = this.Process(expr.Left).ToString();
                        namedArg = this.AdjustNamedArg(string.Format(":{0}", propertyName));

                        //TODO: Depois refatorar isso para ficar geral...
                        if (expr.Left is UnaryExpression && ((UnaryExpression)expr.Left).Operand.Type.GetTypeInfo().IsEnum && expr.Right is ConstantExpression)
                            this.args.Add(namedArg, this.ConvertRightExpressionValue(((UnaryExpression)expr.Left).Operand.Type, ((ConstantExpression)expr.Right).Value));
                        else
                            this.args.Add(namedArg, this.Process(expr.Right));

                        switch (expr.NodeType)
                        {
                            case ExpressionType.NotEqual:
                                return string.Format("({0} <> {1})", propertyName, namedArg);
                            case ExpressionType.GreaterThan:
                                return string.Format("({0} > {1})", propertyName, namedArg);
                            case ExpressionType.GreaterThanOrEqual:
                                return string.Format("({0} >= {1})", propertyName, namedArg);
                            case ExpressionType.LessThan:
                                return string.Format("({0} < {1})", propertyName, namedArg);
                            case ExpressionType.LessThanOrEqual:
                                return string.Format("({0} <= {1})", propertyName, namedArg);
                        }

                        throw new Exception(string.Format("Invalid expression type:{0}.", expr.NodeType));
                    }

                default:
                    throw new Exception(string.Format("Invalid expression type:{0}.", expr.NodeType));
            }
        }
        private string AdjustNamedArg(string propertyName)
        {
            if (this.args.ContainsKey(propertyName))
            {
                for (int i = 0; i < 50; i++)
                {
                    if (!this.args.ContainsKey(propertyName + i.ToString()))
                        return propertyName + i.ToString();
                }

                throw new InvalidOperationException("Too many parameters for a given object property.");
            }

            return propertyName;
        }

        private object ProcessUnaryExpression(UnaryExpression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Not:
                    return string.Format("not {0}", this.Process(expr.Operand));

                case ExpressionType.Convert:
                    if (expr.Operand.Type.GetTypeInfo().IsEnum) // || expr.Operand.Type == typeof(CloudID))
                    {
                        if (expr.Operand is MemberExpression)
                            return string.Format("{0}", this.ProcessMemberExpression((MemberExpression)expr.Operand));
                        else if (expr.Operand is ConstantExpression)
                            return string.Format("{0}", ((ConstantExpression)expr.Operand).Value);
                        else
                            throw new Exception("Invalid expression!");
                    }
                    else if (Nullable.GetUnderlyingType(expr.Type) != null) //is nullable type
                    {
                        return this.ProcessMemberExpression((MemberExpression)expr.Operand);
                    }
                    //else if (expr.Type.Equals(typeof(CloudID)))
                    //{
                    //    return this.Process(expr.Operand).Replace('.', '_');
                    //}
                    break;
            }

            throw new Exception(string.Format("Invalid expression type:{0}.", expr.NodeType));
        }

        private string FullQualifiedMemberName(MemberExpression expr, out Type declaringType)
        {
            declaringType = null;
            Expression expression = expr;
            StringBuilder name = new StringBuilder();

            while (expr.NodeType == ExpressionType.MemberAccess)
            {
                name.Insert(0, "." + expr.Member.Name);

                if (expr.Expression is MemberExpression)
                    expr = expr.Expression as MemberExpression;
                else
                {
                    if (expr.Expression is ParameterExpression)
                    {
                        declaringType = (expr.Expression as ParameterExpression).Type;
                    }
                    break;
                }
            }

            return name.ToString();
        }

        private object ProcessMemberExpression(MemberExpression expr)
        {
            Type declaringType;
            string fullName = this.FullQualifiedMemberName(expr, out declaringType);

            if (this.parameters.Contains(declaringType))//member.DeclaringType))
            {
                string property = fullName.Substring(1);

                if (PropertyResolve != null)
                    property = PropertyResolve(property);

                //return property;
                return (Columns.FindColumnWithPropertyName(property) ?? Columns.MultiColumnPart(expr) ?? Columns.MultiColumnPart(expr)).Name;
            }
            else
            {
                var evaluate = Expression.Lambda(expr).Compile();
                object rightArg = evaluate.DynamicInvoke();

                return this.ConvertRightExpressionValue(expr.Type, rightArg);
                //return Columns.IsMultiColumn(expr) ? rightArg : this.ConvertRightExpressionValue(expr.Type, rightArg);
            }
        }
        private object ConvertRightExpressionValue(Type type, object value)
        {
            if (type == typeof(string))
            {
                return value;
            }
            else if (type == typeof(bool))
            {
                return value; 
            }
            else if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                return value;
            }
            else if (type == typeof(Guid))
            {
                return value;
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                return Enum.ToObject(type, value).ToString();
            }
            else
            {
                return value != null ? value.ToString() : null;
            }
        }
        private object ProcessConstantExpression(ConstantExpression expr)
        {
            return this.ConvertRightExpressionValue(expr.Type, expr.Value);
        }

        private object EvaluateRightBinaryExpression(BinaryExpression expr)
        {
            if (expr.Left is UnaryExpression)
            {
                var expression = expr.Left as UnaryExpression;

                if (expression.Operand.NodeType == ExpressionType.MemberAccess)
                {
                    PropertyInfo property = (expression.Operand as MemberExpression).Member as PropertyInfo;

                    if (property != null && property.PropertyType.GetTypeInfo().IsEnum)
                    {
                        object enumValue = this.EvaluateSideExpression(expr.Right);
                        return Enum.ToObject(property.PropertyType, enumValue);
                    }
                    else
                        return this.EvaluateSideExpression(expr.Right);
                }
                else
                    return this.EvaluateSideExpression(expr.Right);
            }
            else
                return this.EvaluateSideExpression(expr.Right);
        }
        //private string EvaluateBinaryExpression(BinaryExpression expr)
        //{
        //    if (!this.simpleExprs.ContainsKey(expr.NodeType))
        //        throw new Exception(string.Format("Unhandled simple expression type: {0}.", expr.NodeType));

        //    var property = this.EvaluateSideExpression(expr.Left) as string;
        //    object value = this.EvaluateRightBinaryExpression(expr);

        //    return this.simpleExprs[expr.NodeType](property, value);
        //}
        private object EvaluateSideExpression(Expression expression)
        {
            if (expression is UnaryExpression)
            {
                return this.EvaluateUnaryExpression(expression as UnaryExpression);
            }
            else if (expression is ConstantExpression)
            {
                return this.ProcessConstantExpression(expression as ConstantExpression);
            }
            else if (expression is MethodCallExpression)
            {
                throw new NotSupportedException();
            }
            else if (expression is MemberExpression)//(exprType.IsAssignableFrom(typeof(MemberExpression)))
            {
                MemberExpression expr = expression as MemberExpression;
                return this.ProcessMemberExpression(expr);
            }
            else
                throw new NotSupportedException();
        }
        private object EvaluateUnaryExpression(UnaryExpression expression)
        {
            if (expression.Operand.NodeType == ExpressionType.MemberAccess)
            {
                MemberExpression expr = expression.Operand as MemberExpression;

                switch (expression.NodeType)
                {
                    case ExpressionType.Convert:
                        if (this.IsParameterType(expr))
                        {
                            return this.ProcessMemberExpression(expr);
                        }
                        else
                        {
                            var evaluate = Expression.Lambda(expression).Compile();
                            return evaluate.DynamicInvoke();
                        }
                    default:
                        return " not " +
                            this.ProcessMemberExpression(expr).ToString();
                }
            }
            else
                throw new NotSupportedException();
        }

        #endregion

        #region Private utility methods

        #endregion

        private object Process(Expression expression)
        {
            Type exprType = expression.GetType();

            if (expression is BinaryExpression) //(exprType.IsAssignableFrom(typeof(BinaryExpression)))
            {
                BinaryExpression expr = expression as BinaryExpression;
                return this.ProcessBinaryExpression(expr);
            }
            else if (expression is UnaryExpression)
            {
                UnaryExpression expr = expression as UnaryExpression;
                return this.ProcessUnaryExpression(expr);
            }
            else if (expression is LambdaExpression)
            {
                return this.Process((expression as LambdaExpression).Body);
            }
            else if (expression is InvocationExpression)
            {
                return this.Process((expression as InvocationExpression).Expression);
            }
            else if (expression is MemberExpression)
            {
                MemberExpression expr = expression as MemberExpression;

                if (expression.Type == typeof(bool))
                {
                    if (this.IsParameterType(expr)) //Verify if is a expression operating over the parameter type
                    {
                        //return string.Format("{0} == true", (this.ProcessMemberExpression(expr) as string));
                        return this.ProcessMemberExpression(expr) as string;
                    }
                    else //As filter.Birth.HasValue
                    {
                        return ((bool)this.ProcessMemberExpression(expr)).ToString();
                    }
                }
                else
                {
                    //return expr.Member.Name;
                    return this.ProcessMemberExpression(expr);
                }
            }
            else if (expression is ConstantExpression)
            {
                ConstantExpression expr = expression as ConstantExpression;

                return this.ProcessConstantExpression(expr);
                //return (string)this.ProcessConstantExpression(expr);
            }
            else if (expression is MethodCallExpression)
            {
                return this.ProcessMethodCallExpression(expression as MethodCallExpression);
            }
            else
                throw new Exception(string.Format("Expression type '{0}' not supported.", exprType.Name));
        }
        private bool IsParameterType(MemberExpression expr)
        {
            if (expr.Member.DeclaringType.GetTypeInfo().IsPrimitive)
                return this.IsParameterType(expr.Expression as MemberExpression);
            else
            {
                //return this.parameters.Contains(expr.Member.DeclaringType);

                foreach (Type type in this.parameters)
                {
                    if (expr.Member.DeclaringType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                        return true;
                }

                return false;
            }
        }

        public static string Parse<T>(Expression expression, IDictionary<string, object> args)
        {
            var parser = new SQLiteExprParser(new ReadOnlyCollection<ParameterExpression>(new List<ParameterExpression>()), args);
            parser.parameters.Add(typeof(T));

            return parser.Process(expression).ToString();
        }

        public static string Parse(Expression expression, ReadOnlyCollection<ParameterExpression> parameters, IDictionary<string, object> args)
        {
            return (string)new SQLiteExprParser(parameters, args).Process(expression);
        }

        public static Func<string, string> PropertyResolve { get; set; }

        public static IEnumerable<TableMapping.Column> Columns { get; set; }
    }
}
