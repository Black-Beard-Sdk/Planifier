using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Bb.Taskers.UnitTest
{

    [TestClass]
    public class UnitTest1
    {

        // -- node -- node --                
        [TestMethod]
        public void TestSerializable()
        {

            var nodes = new TaskNodes<Context>();

            bool t1 = false;

            var node1 = nodes.Create("task1", ctx =>
            {
                t1 = true;
                return true;
            });

            var node2 = nodes.Create("task2", ctx =>
            {
                Assert.AreEqual(t1, true);
                return true;
            });

            node1.ContinueWith(node2);

            var context = new Context();
            nodes.Run(context)
                .Wait(30);

            Assert.AreEqual(t1, true);

        }

        //               --  node --
        //             /             \
        // -- node -- /               \  -- node
        //            \               /
        //             \             /
        //                -- node -- 
        [TestMethod]
        public void TestParallelOk()
        {

            var nodes = new TaskNodes<Context>();
            bool t1 = false;
            bool t3 = false;
            bool t21 = false;
            bool t22 = false;

            var node1 = nodes.Create("task1", ctx =>
            {
                t1 = true;
                Thread.Sleep(10);
                return true;
            });

            var node21 = nodes.Create("task2-1", ctx =>
            {
                Assert.AreEqual(t1, true);
                Thread.Sleep(80); // La tache est lancée en premier mais est plus longue
                t21 = true;
                return true;
            });

            var node22 = nodes.Create("task2-2", ctx =>
            {
                Assert.AreEqual(t1, true);
                Assert.AreEqual(t21, false);
                Thread.Sleep(20); // On finit avant l'autre tache parallelisée
                t22 = true;
                return true;
            });

            var node3 = nodes.Create("task3", ctx =>
            {
                Assert.AreEqual(t21, true); // On démarre quand les deux taches paralelisées precedentes ont finis
                Assert.AreEqual(t22, true);
                t3 = true;
                return true;
            });


            node1.ContinueWith(node21)
                .ContinueWith(node3)
                ;

            node1.ContinueWith(node22) // Ouai on peut faire ca aussi
                .ContinueWith("task3")
                ;

            var context = new Context();
            nodes.Run(context)
                .Wait(30);

            Assert.AreEqual(t3, true);

        }

        //               --  node --
        //             /             \
        // -- node -- /               \  -- node 
        //            \               /
        //             \             /
        //                -- node X-- 
        [TestMethod]
        public void TestParallelKo()
        {


            var nodes = new TaskNodes<Context>();
            bool t1 = false;
            bool t21 = false;
            bool t22 = false;
            bool result = false;
            var node1 = nodes.Create("task1", ctx =>
            {
                t1 = true;
                Thread.Sleep(10);
                return true;
            });

            var node21 = nodes.Create("task2-1", ctx =>
            {
                Assert.AreEqual(t1, true);
                Thread.Sleep(200);
                t21 = true;
                Assert.AreEqual(t22, true);
                return false;
            });

            var node22 = nodes.Create("task2-2", ctx =>
            {
                Assert.AreEqual(t1, true);
                Assert.AreEqual(t21, false);
                Thread.Sleep(20);
                t22 = true;
                return result;
            });

            var node3 = nodes.Create("task3", ctx =>
            {
                Assert.AreEqual(t21, true);
                Assert.AreEqual(t22, true);
                return true;
            });

            node1.ContinueWith(node21)
                .ContinueWith(node3)
                ;

            node1.ContinueWith(node22) // Ouai on peut faire ca aussi
                .ContinueWith("task3")
                ;

            var context = new Context();
            nodes.Run(context)
                .Wait(30);

            result = true;
            nodes.Reset();

            nodes.Run(context, true)
                .Wait(30);

        }


    }

    public class Context
    {

    }

}
