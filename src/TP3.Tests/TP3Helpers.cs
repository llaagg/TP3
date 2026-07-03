using FakeItEasy;
using TP3.Interfaces;
using TP3.Messages;

public static class TP3Helpers
{
    public static INode SetupNode(string name, NodeType nodeType = NodeType.Directory, List<INode>? children = null)
    {
        var node = A.Fake<INode>();
        A.CallTo(() => node.Name).Returns(name);
        A.CallTo(() => node.NodeType).Returns(nodeType);
        if (children != null)
        {
            A.CallTo(() => node.Children).Returns(children);
        }
        return node;
    }

    
    public static INode MockFileSystem()
    {
        var root = SetupNode("root", NodeType.Directory, new List<INode>()
        {
            SetupNode("FILESYSTEM", NodeType.Directory, new List<INode>()
            {
                SetupNode("bin", NodeType.Directory, new List<INode>()
                {
                    SetupNode("ls", NodeType.File),
                    SetupNode("cat", NodeType.File),
                }),
                SetupNode("usr", NodeType.Directory, new List<INode>()
                {
                    SetupNode("local", NodeType.Directory, new List<INode>()
                    {
                        SetupNode("bin", NodeType.Directory, new List<INode>()
                        {
                            SetupNode("myapp", NodeType.File),
                        }),
                    }),
                }),
            }),
            SetupNode("README.md", NodeType.File),
        });
        return root;
    }

}