using System.Diagnostics;

namespace BerldBlackjack.ConsoleApp
{
    internal class Program
    {
        private static void Main()
        {
            Console.WriteLine("Build nodes");

            List<BaseNode> baseNodes = NodeBuilder.Build();

            Console.WriteLine($"Created: {Node.Created}");
            Console.WriteLine();
            Console.WriteLine("Evaluate");

            NodeEvaluator.Evaluate(baseNodes);

            SplitApproxAddition.Add(baseNodes);

            Console.WriteLine();

            foreach (BaseNode baseNode in baseNodes.OrderBy(c => GetDecisionNode(c)?.Kind).ThenBy(c => c.PlayerSum))
            {
                Node? decisionNode = GetDecisionNode(baseNode);
                Debug.Assert(decisionNode is not null);
                Debug.Assert(baseNode.Ev == decisionNode.Ev);
                Console.WriteLine($"{baseNode}: {decisionNode.Ev:N8} -> {decisionNode.Kind}");
            }

            double averageEv = baseNodes.Sum(c => c.Ratio * c.Ev);
            double totalRatio = baseNodes.Sum(c => c.Ratio);
            averageEv /= totalRatio;

            Console.WriteLine();
            Console.WriteLine($"Created: {Node.Created}");
            Console.WriteLine($"Total ratio: {totalRatio:N8}");
            Console.WriteLine($"Average EV: {averageEv:N8}");
        }

        private static Node? GetDecisionNode(Node node)
        {
            Node? evNode;

            if (node.Children is null)
            {
                evNode = node;
            }
            else
            {
                evNode = node.Children.MaxBy(c => c.child.Ev).child;
            }

            return evNode;
        }
    }
}