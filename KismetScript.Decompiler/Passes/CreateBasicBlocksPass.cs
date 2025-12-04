using KismetScript.Linker.Decompiler.Context;
using KismetScript.Linker.Decompiler.Context.Nodes;
using UAssetAPI.Kismet.Bytecode;

namespace KismetScript.Linker.Decompiler.Passes
{
    public class CreateBasicBlocksPass : IDecompilerPass
    {
        public Node Execute(DecompilerContext context, Node? root)
        {
            var newNodes = new List<Node>();
            var r = root!;

            for (int i = 0; i < r.Children.Count; i++)
            {
                var start = i;
                var end = r.Children.Count - 1;
                for (int j = i; j < r.Children.Count; j++)
                {
                    if (r.Children[j] is JumpNode ||
                        r.Children[j].Source?.Token == EExprToken.EX_PushExecutionFlow ||
                        r.Children[j].Source?.Token == EExprToken.EX_EndOfScript)
                    {
                        // A jump has been found
                        end = j + 1;
                        break;
                    }

                    if (r.Children[j].ReferencedBy.Count != 0 && j != i)
                    {
                        // Something jumps to this
                        end = j;
                        break;
                    }
                }

                var nodes = r.Children
                    .Skip(start)
                    .Take(end - start);
                if (!nodes.Any())
                    continue;
                var blockNode = new BlockNode()
                {
                    Source = null,
                    Parent = nodes.First().Parent,
                    CodeStartOffset = nodes.First().CodeStartOffset,
                    CodeEndOffset = nodes.Last().CodeEndOffset,
                    Children = nodes.ToList(),
                    ReferencedBy = nodes.First().ReferencedBy,
                };

                foreach (var node in blockNode.Children)
                {
                    node.Parent = blockNode;
                }

                newNodes.Add(blockNode);

                i = end - 1;
            }

            r.Children.Clear();
            r.Children.AddRange(newNodes);
            return r;
        }
    }
}
