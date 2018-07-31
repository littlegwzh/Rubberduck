﻿using Rubberduck.Parsing.PreProcessing;
using Rubberduck.VBEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Rubberduck.Parsing.Rewriter;
using Rubberduck.VBEditor.SourceCodeHandling;

namespace Rubberduck.Parsing.VBA
{
    public class SynchronousParseRunner : ParseRunnerBase 
    {
        public SynchronousParseRunner(
            RubberduckParserState state,
            IParserStateManager parserStateManager,
            Func<IVBAPreprocessor> preprocessorFactory,
            IAttributeParser attributeParser,
            ISourceCodeProvider codePaneSourceCodeProvider,
            ISourceCodeProvider attributesSourceCodeProvider,
            IModuleRewriterFactory moduleRewriterFactory) 
        :base(state, 
            parserStateManager, 
            preprocessorFactory, 
            attributeParser,
            codePaneSourceCodeProvider,
            attributesSourceCodeProvider,
            moduleRewriterFactory)
        { }


        public override void ParseModules(IReadOnlyCollection<QualifiedModuleName> modules, CancellationToken token)
        {
            if (!modules.Any())
            {
                return;
            }

            token.ThrowIfCancellationRequested();

            try
            {
                foreach (var module in modules)
                {
                    ParseModule(module, token);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                StateManager.SetStatusAndFireStateChanged(this, ParserState.Error, token);
                throw;
            }
        }
    }
}
