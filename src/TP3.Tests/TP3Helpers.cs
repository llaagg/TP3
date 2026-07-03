using FakeItEasy;
using System.Text;
using TP3.Interfaces;
using TP3.Messages;

public static class TP3Helpers
{
    public static INode SetupNode(string name, NodeType nodeType = NodeType.Directory, List<INode>? children = null)
    {
        var node = A.Fake<INode>();
        A.CallTo(() => node.Id).Returns(Guid.NewGuid().ToString("N").Substring(0, 8));
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
        var moceckFileStream = A.Fake<ITP3DataStream>();
        A.CallTo(() => moceckFileStream.Read(A<ulong>.Ignored, A<ulong>.Ignored))
            .Returns(Task.FromResult(Encoding.UTF8.GetBytes("abcdefghij")));

        var mockedFile = SetupNode("README.md", NodeType.File);
        A.CallTo(() => mockedFile.Get()).Returns(moceckFileStream);

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
            mockedFile,
        });
        return root;
    }

}