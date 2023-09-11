using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArrayPrimesTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
        }

        [TestMethod]
        public void TestAlwaysTrue()
        {
            Assert.AreEqual(true, true);
        }

        [TestMethod]
        public void TestAlwaysFalse()
        {
            Assert.AreEqual(true, false);
        }


    }
}
