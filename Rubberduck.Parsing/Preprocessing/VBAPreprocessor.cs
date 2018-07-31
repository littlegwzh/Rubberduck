﻿using Antlr4.Runtime;
using System.Threading;

namespace Rubberduck.Parsing.PreProcessing
{
    public sealed class VBAPreprocessor : IVBAPreprocessor
    {
        private readonly double _vbaVersion;
        private readonly VBAPrecompilationParser _parser;
        private readonly ICompilationArgumentsProvider _compilationArgumentsProvider;

        public VBAPreprocessor(double vbaVersion, ICompilationArgumentsProvider compilationArgumentsProvider)
        {
            _vbaVersion = vbaVersion;
            _compilationArgumentsProvider = compilationArgumentsProvider;
            _parser = new VBAPrecompilationParser();
        }

        public void PreprocessTokenStream(string projectId, string moduleName, CommonTokenStream tokenStream, BaseErrorListener errorListener, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var symbolTable = new SymbolTable<string, IValue>();
            var tree = _parser.Parse(moduleName, tokenStream, errorListener);
            token.ThrowIfCancellationRequested();
            var stream = tokenStream.TokenSource.InputStream;
            var evaluator = new VBAPreprocessorVisitor(symbolTable, new VBAPredefinedCompilationConstants(_vbaVersion), _compilationArgumentsProvider.UserDefinedCompilationArguments(projectId), stream, tokenStream);
            var expr = evaluator.Visit(tree);
            var processedTokens = expr.Evaluate(); //This does the actual preprocessing of the token stream as a side effect.
            tokenStream.Reset();
        }


    }
}
