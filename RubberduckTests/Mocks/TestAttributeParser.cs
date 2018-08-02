﻿using System;
using System.Threading;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Rubberduck.Parsing.PreProcessing;
using Rubberduck.Parsing.Symbols.ParsingExceptions;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor;

namespace RubberduckTests.Mocks
{
    public class TestAttributeParser : IAttributeParser
    {
        private readonly Func<IVBAPreprocessor> _preprocessorFactory;

        public TestAttributeParser(Func<IVBAPreprocessor> preprocessorFactory)
        {
            _preprocessorFactory = preprocessorFactory;
        }

        public (IParseTree tree, ITokenStream tokenStream) Parse(QualifiedModuleName module, string code, CancellationToken cancellationToken)
        {
            var tokenStreamProvider = new SimpleVBAModuleTokenStreamProvider();
            var tokens = tokenStreamProvider.Tokens(code);
            var preprocessor = _preprocessorFactory();
            var preprocessingErrorListener = new PreprocessorExceptionErrorListener(module.ComponentName, ParsePass.AttributesPass);
            preprocessor.PreprocessTokenStream(null, module.ComponentName, tokens, preprocessingErrorListener, cancellationToken);
            // parse tree isn't usable for declarations because
            // line numbers are offset due to module header and attributes
            // (these don't show up in the VBE, that's why we're parsing an exported file)

            var mainParseErrorListener = new MainParseExceptionErrorListener(module.ComponentName, ParsePass.AttributesPass);
            var parseResults = new VBAModuleParser().Parse(module.Name, tokens, mainParseErrorListener);

            cancellationToken.ThrowIfCancellationRequested();
            return (parseResults.tree, parseResults.tokenStream);
        }
    }
}