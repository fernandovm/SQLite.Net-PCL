using System;
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;

using SQLite.Net.Interop;
using SQLite.Net.Attributes;

#if __WIN32__
using SQLitePlatformTest = SQLite.Net.Platform.Win32.SQLitePlatformWin32;
using System.Reflection;
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
    [TestFixture]
    public class CustomTypeTests
    {
        private SQLiteConnection db;

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

        public class Index
        {
            //[PrimaryKey, MultiColumn("PartitionID", "EntityID")]
            public CloudID ID { get; set; }
            
            public int Width { get; set; }

            public String Text { get; set; }

            public CloudID AccountID { get; set; }

            //public long[] Data { get; set; }

            public override string ToString()
            {
                return string.Format("[Index: ID='{0}', Text='{1}']", this.ID, this.Text);
            }
        }

        [SetUp]
        public void SetupTest()
        {
            this.db = new SQLiteConnection(new SQLitePlatformTest(), TestPath.GetTempFileName()); //, true, new MySerializer());

            //this.db.AttributeProvider = AttributeProvider.NewProvider()
            //    .Bind<Index>(x => x.ID)
            //    .To(new MultiColumnAttribute("PartitionID", "EntityID"));

            this.db.AttributeProvider = AttributeProvider.NewProvider()
                .Bind<Index, TableAttribute>("MyTable")
                .Bind<CloudID, MultiColumnAttribute>("PartitionID", "EntityID")
                .BindName<CloudID, PrimaryKeyAttribute>("ID");

            this.db.CreateTable<Index>(CreateFlags.ImplicitIndex);
        }

        [TearDown]
        public void TearDownTest()
        {
            this.db.Close();
        }

        [Test]
        public void SimpleInsertCustomTypeTest()
        {
            var id = new CloudID("AAA", "BBB");

            var obj1 = new Index
            {
                ID = id,
                Width = 15,
                Text = "First Guid Object"
            };

            int numIn1 = db.Insert(obj1);
            Assert.AreEqual(1, numIn1);
        }

        [Test]
        public void SimpleUpdateCustomTypeTest()
        {
            this.SimpleInsertCustomTypeTest();

            var text = "First Guid Object XX";
            var id = new CloudID("AAA", "BBB");
            var obj1 = new Index { ID = id, Text = "First Guid Object" };

            //var obj = db.Find<Index>(id);
            var obj = db.Table<Index>().FirstOrDefault();
            //var obj = db.Query<Index>("select * from Index").ToList().FirstOrDefault();
            Assert.IsNotNull(obj);

            obj.Text = text;
            int numIn1 = db.Update(obj);
            Assert.AreEqual(1, numIn1);

            var obj2 = db.Table<Index>().FirstOrDefault();
            //var obj2 = db.Query<Index>("select * from Index").ToList().FirstOrDefault();
            Assert.IsNotNull(obj2);

            Assert.AreEqual(text, obj2.Text);
            Assert.AreEqual(obj1.ID.EntityID, obj2.ID.EntityID);
            Assert.AreEqual(obj1.ID.PartitionID, obj2.ID.PartitionID);
        }

        [Test]
        public void SimpleSelectCustomTypeTest()
        {
            this.SimpleInsertCustomTypeTest();

            var id = new CloudID("AAA", "BBB");
            var obj1 = new Index { ID = id, Text = "First Guid Object" };

            var obj = db.Table<Index>().FirstOrDefault();
            //var obj = db.Query<Index>("select * from Index").ToList().FirstOrDefault();
            Assert.IsNotNull(obj);

            Assert.AreEqual(obj1.Text, obj.Text);
            Assert.AreEqual(obj1.ID.EntityID, obj.ID.EntityID);
            Assert.AreEqual(obj1.ID.PartitionID, obj.ID.PartitionID);
        }

        [Test]
        public void SimpleQueryCustomTypeByIDTest()
        {
            this.SimpleInsertCustomTypeTest();

            var text = "First Guid Object";
            var id = new CloudID("AAA", "BBB");
            var obj1 = new Index { ID = id, Text = text };

            var obj = db.Table<Index>().Where(x => x.ID == id).ToList().FirstOrDefault();
            Assert.IsNotNull(obj);

            Assert.AreEqual(obj1.Text, obj.Text);
            Assert.AreEqual(obj1.ID.EntityID, obj.ID.EntityID);
            Assert.AreEqual(obj1.ID.PartitionID, obj.ID.PartitionID);
        }

        [Test]
        public void MultiQueryCustomTypeByIDTest()
        {
            this.SimpleInsertCustomTypeTest();

            var text = "First Guid Object";
            var id = new CloudID("AAA", "BBB");
            var obj1 = new Index { ID = id, Text = text };

            var obj = db.Table<Index>().Where(x => x.ID == id && x.Width > 10).ToList().FirstOrDefault();
            Assert.IsNotNull(obj);

            Assert.AreEqual(obj1.Text, obj.Text);
            Assert.AreEqual(obj1.ID.EntityID, obj.ID.EntityID);
            Assert.AreEqual(obj1.ID.PartitionID, obj.ID.PartitionID);
        }

        [Test]
        public void SimpleQueryCustomTypeByPartitionIDTest()
        {
            this.SimpleInsertCustomTypeTest();

            var text = "First Guid Object";
            var id = new CloudID("AAA", "BBB");
            var obj1 = new Index { ID = id, Text = text };

            //var obj = db.Table<Index>().Where(x => x.Text == text).ToList().FirstOrDefault();
            //Assert.IsNotNull(obj);

            var partitionID = id.PartitionID;
            var obj = db.Table<Index>().Where(x => x.ID.PartitionID == partitionID).ToList().FirstOrDefault();
            Assert.IsNotNull(obj);

            Assert.AreEqual(obj1.Text, obj.Text);
            Assert.AreEqual(obj1.ID.EntityID, obj.ID.EntityID);
            Assert.AreEqual(obj1.ID.PartitionID, obj.ID.PartitionID);
        }

        [Test]
        public void SimpleQueryCustomTypeByEntityIDTest()
        {
            this.SimpleInsertCustomTypeTest();

            var text = "First Guid Object";
            var id = new CloudID("AAA", "BBB");
            var obj1 = new Index { ID = id, Text = text };

            //var obj = db.Table<Index>().Where(x => x.Text == text).ToList().FirstOrDefault();
            //Assert.IsNotNull(obj);

            var entityID = id.EntityID;
            //var obj = db.Table<Index>().Where(x => x.ID == id).ToList().FirstOrDefault();
            var obj = db.Table<Index>().Where(x => x.ID.EntityID == entityID).ToList().FirstOrDefault();
            Assert.IsNotNull(obj);

            Assert.AreEqual(obj1.Text, obj.Text);
            Assert.AreEqual(obj1.ID.EntityID, obj.ID.EntityID);
            Assert.AreEqual(obj1.ID.PartitionID, obj.ID.PartitionID);
        }

        [Test]
        public void SimpleQueryCustomTypeUsingMethodTest()
        {
            this.SimpleInsertCustomTypeTest();

            var text = "First Guid Object";
            var id = new CloudID("AAA", "BBB");
            var obj1 = new Index { ID = id, Text = text };

            var entityID = id.EntityID;
            var obj = db.Table<Index>().Where(x => x.ID.EntityID.Eq(entityID)).ToList().FirstOrDefault();
            Assert.IsNotNull(obj);

            Assert.AreEqual(obj1.Text, obj.Text);
            Assert.AreEqual(obj1.ID.EntityID, obj.ID.EntityID);
            Assert.AreEqual(obj1.ID.PartitionID, obj.ID.PartitionID);
        }

        [Test]
        public void MultiPredQueryCustomTypeTest()
        {
            this.SimpleInsertCustomTypeTest();

            var text = "First Guid Object";
            var id = new CloudID("AAA", "BBB");
            var obj1 = new Index { ID = id, Text = text };

            var partitionID = id.PartitionID;
            var obj = db.Table<Index>().Where(x => x.ID.PartitionID == partitionID && x.Width > 10).ToList().FirstOrDefault();
            Assert.IsNotNull(obj);

            Assert.AreEqual(obj1.Text, obj.Text);
            Assert.AreEqual(obj1.ID.EntityID, obj.ID.EntityID);
            Assert.AreEqual(obj1.ID.PartitionID, obj.ID.PartitionID);
        }

        //var tutors = this.relationRepository.Query(
        //    r =>
        //        r.ID.PartitionID.Eq(evt.AccountID.PartitionID) &&
        //        r.Direction == RelationshipDirection.Invited &&
        //        r.Type == RelationshipType.Tutor &&
        //        r.Status == RelationshipStatus.Active
        //    );

    }

    public class MySerializer : IBlobSerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            return new byte[0];
        }

        public object Deserialize(byte[] data, Type type)
        {
            return Activator.CreateInstance(type);
        }

        public bool CanDeserialize(Type type)
        {
            return true;
        }
    }
}