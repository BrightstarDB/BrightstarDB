using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestClass]
    public class PropertyChangeNotificationTests
    {
        private MyEntityContext _context;
        private string _storeName;
        private ICompany _company;
        private IMarket _ftse, _nyse;
        private string _lastPropertyChanged;
        private IFoafPerson _person;
        private NotifyCollectionChangedEventArgs _lastCollectionChangeEvent;

        public PropertyChangeNotificationTests()
        {
            _storeName = "PropertyChangeNotificationTests_" + DateTime.UtcNow.Ticks;
            _context = new MyEntityContext("type=embedded;storesDirectory=c:\\brightstar;storeName="+_storeName);
            _ftse = _context.Markets.Create();
            _nyse = _context.Markets.Create();
            _company = _context.Companies.Create();
            _company.Name = "Glaxo";
            _company.HeadCount = 20000;
            _company.PropertyChanged += HandlePropertyChanged;
            _person = _context.FoafPersons.Create();
            (_person.MboxSums as INotifyCollectionChanged).CollectionChanged += HandleCollectionChanged;
            _context.SaveChanges();
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _lastCollectionChangeEvent = e;
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _lastPropertyChanged = e.PropertyName;
        }

        

        [TestCleanup]
        public void TestCleanUp()
        {
            _lastPropertyChanged = null;
        }

        [TestMethod]
        public void TestStringPropertySetAndChanged()
        {
            _lastPropertyChanged = null;
            _company.TickerSymbol = "GLX";
            Assert.AreEqual("TickerSymbol", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.TickerSymbol = "GLXO";
            Assert.AreEqual("TickerSymbol", _lastPropertyChanged);
            
            _lastPropertyChanged = null;
            _company.TickerSymbol = "GLXO"; // No event fired when setting property to the same value
            Assert.IsNull(_lastPropertyChanged);
            
            _lastPropertyChanged = null;
            _company.TickerSymbol = null;
            Assert.AreEqual("TickerSymbol", _lastPropertyChanged);
            
            _lastPropertyChanged = null;
            _company.TickerSymbol = null;
            Assert.IsNull(_lastPropertyChanged);

            _lastPropertyChanged = null;
        }

        [TestMethod]
        public void TestIntegerPropertyChanged()
        {
            _lastPropertyChanged = null;
            _company.HeadCount = 25000;
            Assert.AreEqual("HeadCount", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.HeadCount = 25000;
            Assert.IsNull(_lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.HeadCount = 0;
            Assert.AreEqual("HeadCount", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.HeadCount = 0;
            Assert.IsNull(_lastPropertyChanged);

            _company.HeadCount = 15000;
            Assert.AreEqual("HeadCount", _lastPropertyChanged);
        }

        [TestMethod]
        public void TestRelatedEntityChanged()
        {
            _lastPropertyChanged = null;
            _company.ListedOn = _nyse;
            Assert.AreEqual("ListedOn", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.ListedOn = _nyse;
            Assert.IsNull(_lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.ListedOn = _ftse;
            Assert.AreEqual("ListedOn", _lastPropertyChanged);

            _lastPropertyChanged = null;
            _company.ListedOn = null;
            Assert.AreEqual("ListedOn", _lastPropertyChanged);
        }

        [TestMethod]
        public void TestLiteralCollectionChangeEvents()
        {
            _lastCollectionChangeEvent = null;
            _person.MboxSums.Add("mboxsum1");
            Assert.IsNotNull(_lastCollectionChangeEvent);
            Assert.AreEqual(NotifyCollectionChangedAction.Add, _lastCollectionChangeEvent.Action);
            Assert.AreEqual(_lastCollectionChangeEvent.NewItems[0], "mboxsum1");

            _person.MboxSums.Add("mboxsum2");
            Assert.IsNotNull(_lastCollectionChangeEvent);
            Assert.AreEqual(NotifyCollectionChangedAction.Add, _lastCollectionChangeEvent.Action);
            Assert.AreEqual(_lastCollectionChangeEvent.NewItems[0], "mboxsum2");

            _person.MboxSums.Remove("mboxsum1");
            Assert.IsNotNull(_lastCollectionChangeEvent);
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, _lastCollectionChangeEvent.Action);
            Assert.AreEqual(_lastCollectionChangeEvent.OldItems[0], "mboxsum1");

            _person.MboxSums.Clear();
            Assert.IsNotNull(_lastCollectionChangeEvent);
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, _lastCollectionChangeEvent.Action);

            _lastCollectionChangeEvent = null;
            var friend = _context.FoafPersons.Create();
            (friend.KnownBy as INotifyCollectionChanged).CollectionChanged += HandleCollectionChanged;
            _person.Knows.Add(friend);
            Assert.IsNotNull(_lastCollectionChangeEvent);
            Assert.AreEqual(NotifyCollectionChangedAction.Add, _lastCollectionChangeEvent.Action);
            Assert.AreEqual(_person, _lastCollectionChangeEvent.NewItems[0]);

            _lastCollectionChangeEvent = null;
            (friend.KnownBy as INotifyCollectionChanged).CollectionChanged -= HandleCollectionChanged;
            (_person.Knows as INotifyCollectionChanged).CollectionChanged += HandleCollectionChanged;
            _person.Knows.Remove(friend);
            Assert.IsNotNull(_lastCollectionChangeEvent);
            Assert.AreEqual(NotifyCollectionChangedAction.Remove, _lastCollectionChangeEvent.Action);
            Assert.AreEqual(friend, _lastCollectionChangeEvent.OldItems[0]);

        }
    }
}
