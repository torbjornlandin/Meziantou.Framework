﻿using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Framework.StronglyTypedId;
internal sealed class CSharpGeneratedFileWriter
{
    private readonly StringBuilder _stringBuilder = new(capacity: 2000);
    private bool _mustIndent = true;
    private bool _isEndOfBlock;

    public string IndentationString { get; set; } = "\t";
    public int Indentation { get; set; }

    public void WriteLine()
    {
        _stringBuilder.Append('\n');
        _mustIndent = true;
        _isEndOfBlock = false;
    }

    public void WriteLine(string text)
    {
        if (_isEndOfBlock)
        {
            _isEndOfBlock = false;
            if (text is not "}" and not "else")
            {
                WriteLine();
            }
        }

        WriteIndentation();
        _stringBuilder.EnsureCapacity(text.Length + 1);
        _stringBuilder.Append(text);
        _stringBuilder.Append('\n');
        _mustIndent = true;
        _isEndOfBlock = text == "}";
    }

    public void WriteLine(char text)
    {
        if (_isEndOfBlock)
        {
            _isEndOfBlock = false;
            if (text != '}')
            {
                WriteLine();
            }
        }

        WriteIndentation();
        _stringBuilder.EnsureCapacity(2);
        _stringBuilder.Append(text);
        _stringBuilder.Append('\n');
        _mustIndent = true;
        _isEndOfBlock = text == '}';
    }

    public void Write(char text)
    {
        WriteIndentation();
        _stringBuilder.Append(text);
    }

    public void Write(string text)
    {
        WriteIndentation();
        _stringBuilder.Append(text);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "")]
    public IDisposable BeginPartialContext(ITypeSymbol type, Action<CSharpGeneratedFileWriter>? writeAttributes = null, string? baseTypes = null)
    {
        var initialIndentation = Indentation;
        var ns = GetNamespace(type.ContainingNamespace);
        if (ns != null)
        {
            WriteLine("namespace " + ns);
            BeginBlock();
        }

        WriteContainingTypes(type.ContainingType);
        writeAttributes?.Invoke(this);
        WriteBeginType(type, baseTypes);
        return new CloseBlock(this, Indentation - initialIndentation);

        void WriteContainingTypes(ITypeSymbol? containingType)
        {
            if (containingType == null)
                return;

            WriteContainingTypes(containingType.ContainingType);
            WriteBeginType(containingType, baseTypes: null);
        }

        void WriteBeginType(ITypeSymbol typeSymbol, string? baseTypes)
        {
            var text = typeSymbol switch
            {
                { IsValueType: false, IsRecord: false } => "partial class " + typeSymbol.Name,
                { IsValueType: false, IsRecord: true } => "partial record " + typeSymbol.Name,
                { IsValueType: true, IsRecord: false } => "partial struct " + typeSymbol.Name,
                { IsValueType: true, IsRecord: true } => "partial record struct " + typeSymbol.Name,
            };

            Write(text);
            if (baseTypes != null)
            {
                Write(" : ");
                Write(baseTypes);
            }

            WriteLine();
            _ = BeginBlock();
        }

        static string? GetNamespace(INamespaceSymbol ns)
        {
            string? str = null;
            while (ns != null && !ns.IsGlobalNamespace)
            {
                if (str != null)
                {
                    str = '.' + str;
                }

                str = ns.Name + str;
                ns = ns.ContainingNamespace;
            }

            return str;
        }
    }

    private void WriteIndentation()
    {
        if (!_mustIndent)
            return;

        for (var i = 0; i < Indentation; i++)
        {
            _stringBuilder.Append(IndentationString);
        }

        _mustIndent = false;
    }

    public IDisposable BeginBlock(string value)
    {
        WriteLine(value);
        WriteLine('{');
        Indentation++;
        return new CloseBlock(this, 1);
    }

    public IDisposable BeginBlock()
    {
        WriteLine('{');
        Indentation++;
        return new CloseBlock(this, 1);
    }

    public void EndBlock()
    {
        Indentation--;
        WriteLine('}');
    }

    public SourceText ToSourceText() => SourceText.From(_stringBuilder.ToString(), Encoding.UTF8);

    private sealed class CloseBlock : IDisposable
    {
        private readonly CSharpGeneratedFileWriter _writer;
        private readonly int _count;

        public CloseBlock(CSharpGeneratedFileWriter writer, int count)
        {
            _writer = writer;
            _count = count;
        }

        public void Dispose()
        {
            for (var i = 0; i < _count; i++)
            {
                _writer.EndBlock();
            }
        }
    }
}
