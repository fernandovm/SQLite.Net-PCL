using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using NUnit.Framework;

using SQLite.Net;
using SQLite.Net.Interop;
using SQLite.Net.Platform;
using SQLite.Net.Attributes;

#if __WIN32__
using SQLitePlatformTest = SQLite.Net.Platform.Win32.SQLitePlatformWin32;
#elif WINDOWS_PHONE
using SQLitePlatformTest = SQLite.Net.Platform.WindowsPhone8.SQLitePlatformWP8;
#elif __WINRT__
using SQLitePlatformTest = SQLite.Net.Platform.WinRT.SQLitePlatformWinRT;
#elif __IOS__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinIOS.SQLitePlatformIOS;
#elif __ANDROID__
using SQLitePlatformTest = SQLite.Net.Platform.XamarinAndroid.SQLitePlatformAndroid;
#else
using SQLitePlatformTest = SQLite.Net.Platform.Generic.SQLitePlatformGeneric;
#endif

namespace SQLite.Net.Tests
{
    /// <summary>
    /// 
    ///</summary>
    [TestFixture]
    public class SQLiteExprParserTest
    {
        private IDictionary<string, object> args;
        public ISQLitePlatform Platform { get; private set; }

        [SetUp]
        public void MyTestInitialize()
        {
            Platform = new SQLitePlatformTest();
            this.args = new Dictionary<string, object>();
            var props = Platform.ReflectionService.GetPublicInstanceProperties(typeof(Entity));

            var attrProvider = AttributeProvider.NewProvider()
                .Bind<Entity, TableAttribute>("MyEntity")
                .Bind<CloudID, MultiColumnAttribute>("PartitionID", "EntityID")
                .BindName<CloudID, PrimaryKeyAttribute>("ID");
            
            SQLiteExprParser.Columns = new TableMapping(typeof(Entity), props, CreateFlags.ImplicitIndex, attrProvider).Columns;
        }

        [TearDown]
        public void MyTestCleanup()
        {
        }

        [Test]
        public void SingleBooleanPropertyTest()
        {
            Expression<Func<IEntity, bool>> expr = e => e.Deleted == true;
            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(Deleted = :Deleted)", filter);

            Assert.AreEqual(true, args[":Deleted"]);

            //Ainda não está funcionando... 
            //expr = e => e.Deleted;
            //filter = SQLiteExprParser.Parse(expr, expr.Parameters);
            //Assert.AreEqual("Deleted eq true", filter);
        }

        [Test]
        public void SingleEqualOperatorOnStringTest()
        {
            string myStr = "FernandoVM";

            Expression<Func<IEntity, bool>> expr = e => e.Nome == myStr;
            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(Nome = :Nome)", filter);

            Assert.AreEqual("FernandoVM", args[":Nome"]);
        }

        [Test]
        public void SingleEqualMethodOnStringTest()
        {
            string myStr = "FernandoVM";

            Expression<Func<IEntity, bool>> expr = e => e.Nome.Eq(myStr);
            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(Nome = :Nome)", filter);

            Assert.AreEqual("FernandoVM", args[":Nome"]);
        }

        [Test]
        public void SingleNotEqualMethodOnStringTest()
        {
            string myStr = "FernandoVM";

            Expression<Func<IEntity, bool>> expr = e => e.Nome.Ne(myStr);
            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(Nome <> :Nome)", filter);

            Assert.AreEqual("FernandoVM", args[":Nome"]);
        }

        [Test]
        public void SingleGreaterMethodOnStringTest()
        {
            string myStr = "FernandoVM";

            Expression<Func<IEntity, bool>> expr = e => e.Nome.Gt(myStr);
            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(Nome > :Nome)", filter);

            Assert.AreEqual("FernandoVM", args[":Nome"]);
        }

        [Test]
        public void SingleGreaterOrEqualMethodOnStringTest()
        {
            string myStr = "FernandoVM";

            Expression<Func<IEntity, bool>> expr = e => e.Nome.Ge(myStr);
            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(Nome >= :Nome)", filter);

            Assert.AreEqual("FernandoVM", args[":Nome"]);
        }

        [Test]
        public void SingleLessMethodOnStringTest()
        {
            string myStr = "FernandoVM";

            Expression<Func<IEntity, bool>> expr = e => e.Nome.Lt(myStr);
            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(Nome < :Nome)", filter);

            Assert.AreEqual("FernandoVM", args[":Nome"]);
        }

        [Test]
        public void SingleLessOrEqualMethodOnStringTest()
        {
            string myStr = "FernandoVM";

            Expression<Func<IEntity, bool>> expr = e => e.Nome.Le(myStr);
            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(Nome <= :Nome)", filter);

            Assert.AreEqual("FernandoVM", args[":Nome"]);
        }

        [Test]
        public void SingleEnumPropertyTest()
        {
            Expression<Func<IEntity, bool>> expr = e => e.Type == TypeTest.Const1;
            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(Type = :Type)", filter);

            Assert.AreEqual("Const1", args[":Type"]);
        }

        [Test]
        public void BinaryExpressionWithEnumPropertyTest()
        {
            Expression<Func<IEntity, bool>> expr = e => e.Type == TypeTest.Const1 && e.Direction == DirectionType.Left;

            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("((Type = :Type) and (Direction = :Direction))", filter);

            Assert.AreEqual("Const1", args[":Type"]);
            Assert.AreEqual("Left", args[":Direction"]);
        }

        [Test]
        public void ComposedBinaryExpressionWithEnumPropertyTest()
        {
            Expression<Func<IEntity, bool>> expr1 = e => e.Type == TypeTest.Const1;
            Expression<Func<IEntity, bool>> expr2 = e => e.Direction == DirectionType.Left;
            Expression<Func<IEntity, bool>> expr3 = e => e.Type == TypeTest.Const1 && e.Direction == DirectionType.Left;

            Expression<Func<IEntity, bool>> expr = expr1.And(expr2);

            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("((Type = :Type) and (Direction = :Direction))", filter);

            Assert.AreEqual("Const1", args[":Type"]);
            Assert.AreEqual("Left", args[":Direction"]);
        }

        [Test]
        public void ParameterConversionTest()
        {
            //Expression<Func<IEntity, bool>> expr = e => (e.Type == TypeTest.Const1) && (e.Direction == DirectionType.Left);
            //var expr2 = ExpressionConverter.Transform<IEntity, Entity>(expr);

            //string filter = SQLiteExprParser.Parse(expr, expr.Parameters);
            //Assert.AreEqual("((Type eq 'Const1') and (Direction eq 'Left'))", filter);
        }

        [Test]
        public void SingleCloudIDPropertyTest()
        {
            CloudID cloudID = new CloudID("pID", "eID");
            Expression<Func<IEntity, bool>> expr = e => e.RefID == cloudID;

            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("((RefID_PartitionID = :RefID_PartitionID) and (RefID_EntityID = :RefID_EntityID))", filter);

            Assert.AreEqual("pID", args[":RefID_PartitionID"]);
            Assert.AreEqual("eID", args[":RefID_EntityID"]);
        }

        [Test]
        public void SingleCloudIDPartitionPropertyTest()
        {
            CloudID cloudID = new CloudID("pID", "eID");
            Expression<Func<IEntity, bool>> expr = e => e.RefID.PartitionID == cloudID.PartitionID;

            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(RefID_PartitionID = :RefID_PartitionID)", filter);
            //TODO: Fazer teste também para Expression<Func<IEntity, bool>> expr = e => e.ID.PartitionID == cloudID;

            Assert.AreEqual("pID", args[":RefID_PartitionID"]);
        }

        [Test]
        public void SingleCloudIDPartitionEqPropertyTest()
        {
            CloudID cloudID = new CloudID("pID", "eID");
            Expression<Func<IEntity, bool>> expr = e => e.RefID.PartitionID.Eq(cloudID.PartitionID);
            //TODO: Fazer teste também para Expression<Func<IEntity, bool>> expr = e => e.ID.PartitionID.Eq(cloudID);

            string filter = SQLiteExprParser.Parse(expr, expr.Parameters, args);
            Assert.AreEqual("(RefID_PartitionID = :RefID_PartitionID)", filter);

            Assert.AreEqual("pID", args[":RefID_PartitionID"]);
        }
    }

    public enum TypeTest { Const1, Const2 }
    public enum DirectionType { Left, Right }

    public class CloudID
    {
        public string PartitionID { get; set; }
        public string EntityID { get; set; }

        public CloudID(string partitionID, string entityID)
        {
            this.PartitionID = partitionID;
            this.EntityID = entityID;
        }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", this.PartitionID, this.EntityID);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IEntity //: ITableEntity
    {
        string Nome { get; }
        CloudID RefID { get; }
        bool Deleted { get; set; }
        TypeTest Type { get; set; }
        DirectionType Direction { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Entity : IEntity
    {
        #region IEntity Members

        public string Nome { get; private set; }

        public CloudID RefID { get; private set; }

        public TypeTest Type { get; set; }

        public DirectionType Direction { get; set; }

        #endregion

        #region ITableEntity Members

        public CloudID ID { get; private set; }

        public bool Deleted { get; set; }

        public DateTime CreatedOn { get; private set; }

        public DateTime? DeletedAt { get; private set; }

        #endregion
    }

    public static class Extensions
    {
        /// <summary>
        /// Equal
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool Eq(this string str, string other)
        {
            return str.CompareTo(other) == 0;
        }
        /// <summary>
        /// NotEqual
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool Ne(this string str, string other)
        {
            return !Eq(str, other);
        }

        /// <summary>
        /// GreaterThan
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool Gt(this string str, string other)
        {
            return str.CompareTo(other) > 0;
        }
        /// <summary>
        /// GreaterThanOrEqual
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool Ge(this string str, string other)
        {
            return Eq(str, other) || Gt(str, other);
        }

        /// <summary>
        /// LessThan
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool Lt(this string str, string other)
        {
            return str.CompareTo(other) < 0;
        }
        /// <summary>
        /// LessThanOrEqual
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool Le(this string str, string other)
        {
            return Eq(str, other) || Lt(str, other);
        }

        private static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.AndAlso);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }
    
    }

    public class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> map;

        public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
        {
            this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
        {
            return new ParameterRebinder(map).Visit(exp);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            ParameterExpression replacement;
            if (map.TryGetValue(p, out replacement))
            {
                p = replacement;
            }
            return base.VisitParameter(p);
        }
    }
}
