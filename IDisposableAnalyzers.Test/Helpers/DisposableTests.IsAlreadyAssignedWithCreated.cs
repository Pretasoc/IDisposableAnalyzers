namespace IDisposableAnalyzers.Test.Helpers
{
    using System.Threading;
    using Gu.Roslyn.Asserts;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;

    public partial class DisposableTests
    {
        public class IsAlreadyAssignedWithCreated
        {
            [Test]
            public void FieldAssignedInCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class C
    {
        private Disposable disposable;

        internal C()
        {
            this.disposable = new Disposable();
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("this.disposable = new Disposable()").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void FieldAssignedInLambdaCtor()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        private Disposable disposable;

        public C()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                this.disposable = new Disposable();
            };
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.Yes, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void LocalSeparateDeclarationAndAssignment()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    internal class C
    {
        internal C()
        {
            IDisposable disposable;
            disposable = new Disposable();
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void LocalSeparateDeclarationAndAssignmentInLambda()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C()
        {
            Console.CancelKeyPress += (o, e) =>
            {
                IDisposable disposable;
                disposable = new Disposable();
            };
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void LocalAssignmentInLambda()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;

    public class C
    {
        public C()
        {
            IDisposable disposable;
            Console.CancelKeyPress += (o, e) =>
            {
                disposable = new Disposable();
            };
        }
    }

    public class Disposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("disposable = new Disposable()").Left;
                Assert.AreEqual(Result.Yes, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void FieldAfterEarlyReturn()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        private FileStream stream;

        public bool M(string fileName)
        {
            if (File.Exists(fileName))
            {
                this.stream = File.OpenRead(fileName);
                return true;
            }

            this.stream = null;
            return false;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("this.stream = null").Left;
                Assert.AreEqual(Result.Yes, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void PropertyAfterEarlyReturn()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        public FileStream Stream { get; private set; }

        public bool M(string fileName)
        {
            if (File.Exists(fileName))
            {
                this.Stream = File.OpenRead(fileName);
                return true;
            }

            this.Stream = null;
            return false;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("this.Stream = null").Left;
                Assert.AreEqual(Result.Yes, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void ParameterAfterEarlyReturn()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        private static bool TryGetStream(string fileName, out Stream stream)
        {
            if (File.Exists(fileName))
            {
                stream = File.OpenRead(fileName);
                return true;
            }

            stream = null;
            return false;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("stream = null").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void ParameterBeforeEarlyReturn()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        private static bool TryGetStream(string fileName, out Stream stream)
        {
            if (File.Exists(fileName))
            {
                stream = File.OpenRead(fileName);
                stream = default(Stream);
                return true;
            }

            stream = null;
            return false;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("stream = default(Stream)").Left;
                Assert.AreEqual(Result.Yes, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void LocalAfterEarlyReturn()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        private static bool M(string fileName)
        {
            Stream stream;
            if (File.Exists(fileName))
            {
                stream = File.OpenRead(fileName);
                return true;
            }

            stream = null;
            return false;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("stream = null").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void LocalBeforeEarlyReturn()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System.IO;

    public class C
    {
        private static bool M(string fileName)
        {
            Stream stream;
            if (File.Exists(fileName))
            {
                stream = File.OpenRead(fileName);
                stream = default(Stream);
                return true;
            }

            stream = null;
            return false;
        }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("stream = default(Stream)").Left;
                Assert.AreEqual(Result.Yes, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void OutParameterInExpressionBody()
            {
                var testCode = @"
namespace RoslynSandbox
{
    using System;
    using System.IO;

    public class C
    {
        public void M(out IDisposable disposable) => disposable = File.OpenRead(string.Empty);
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("disposable = File.OpenRead(string.Empty)").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }

            [Test]
            public void Repro()
            {
                var testCode = @"
namespace RoslynSandbox
{
    public class C
    {
        private C1 c1;
        private C1 c2;

        public C1 P1
        {
            get
            {
                return this.c1;
            }

            set
            {
                if (Equals(value, this.c1))
                {
                    return;
                }

                if (value != null && this.c2 != null)
                {
                    this.P2 = null;
                }

                if (this.c1 != null)
                {
                    this.c1.Selected = false;
                }

                this.c1 = value;
                if (this.c1 != null)
                {
                    this.c1.Selected = true;
                }
            }
        }

        public C1 P2
        {
            get
            {
                return this.c2;
            }

            set
            {
                if (Equals(value, this.c2))
                {
                    return;
                }

                if (value != null && this.c1 != null)
                {
                    this.P1 = null;
                }

                if (this.c2 != null)
                {
                    this.c2.Selected = false;
                }

                this.c2 = value;
                if (this.c2 != null)
                {
                    this.c2.Selected = true;
                }
            }
        }
    }

    public class C1
    {
        public bool Selected { get; set; }
    }
}";
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var compilation = CSharpCompilation.Create("test", new[] { syntaxTree }, MetadataReferences.FromAttributes());
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var value = syntaxTree.FindAssignmentExpression("this.P1 = null;").Left;
                Assert.AreEqual(Result.No, Disposable.IsAlreadyAssignedWithCreated(value, semanticModel, CancellationToken.None, out _));
            }
        }
    }
}
