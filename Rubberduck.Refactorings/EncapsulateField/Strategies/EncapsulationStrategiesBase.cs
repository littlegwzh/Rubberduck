﻿using Rubberduck.Parsing.Rewriter;
using Rubberduck.SmartIndenter;
using Rubberduck.VBEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rubberduck.Refactorings.EncapsulateField.Strategies
{
    public interface IEncapsulateFieldStrategy
    {
        IExecutableRewriteSession RefactorRewrite(EncapsulateFieldModel model, IExecutableRewriteSession rewriteSession);
        void InsertNewContent(int? codeSectionStartIndex, EncapsulateFieldModel model, IExecutableRewriteSession rewriteSession, bool includePreviewMessage = false);
        Dictionary<string, IEncapsulateFieldCandidate> UdtMemberTargetIDToParentMap { get; set; }
    }

    public abstract class EncapsulateFieldStrategiesBase : IEncapsulateFieldStrategy
    {
        public EncapsulateFieldStrategiesBase(QualifiedModuleName qmn, IIndenter indenter)
        {
            TargetQMN = qmn;
            Indenter = indenter;
        }

        protected QualifiedModuleName TargetQMN {private set; get;}

        protected IIndenter Indenter { private set; get; }

        public  IExecutableRewriteSession RefactorRewrite(EncapsulateFieldModel model, IExecutableRewriteSession rewriteSession)
        {
            var nonUdtMemberFields = model.FlaggedEncapsulationFields
                    .Where(encFld => encFld.Declaration.IsVariable());

            foreach (var nonUdtMemberField in nonUdtMemberFields)
            {
                var attributes = nonUdtMemberField.EncapsulationAttributes;
                ModifyEncapsulatedVariable(nonUdtMemberField, attributes, rewriteSession); //, model.EncapsulateWithUDT);
                RenameReferences(nonUdtMemberField, attributes.PropertyName ?? nonUdtMemberField.Declaration.IdentifierName, rewriteSession);
            }

            var rewriter = EncapsulateFieldRewriter.CheckoutModuleRewriter(rewriteSession, TargetQMN);
            RewriterRemoveWorkAround.RemoveDeclarationsFromVariableLists(rewriter);

            return rewriteSession;
        }

        public Dictionary<string, IEncapsulateFieldCandidate> UdtMemberTargetIDToParentMap { get; set; }

        public void InsertNewContent(int? codeSectionStartIndex, EncapsulateFieldModel model, IExecutableRewriteSession rewriteSession, bool includePreviewMessage = false)
        {
            var rewriter = EncapsulateFieldRewriter.CheckoutModuleRewriter(rewriteSession, TargetQMN);

            //var newContent = model.NewContent();

            var newContent1 = new EncapsulateFieldNewContent();
            newContent1 = LoadNewDeclarationsContent(newContent1, model.FlaggedEncapsulationFields);

            if (includePreviewMessage)
            {
                var postScript = "'<===== No Changes below this line =====>";
                newContent1 = LoadNewPropertiesContent(newContent1, model.FlaggedEncapsulationFields, postScript);
            }
            else
            {
                newContent1 = LoadNewPropertiesContent(newContent1, model.FlaggedEncapsulationFields);
            }

            rewriter.InsertNewContent(codeSectionStartIndex, newContent1);

        }

        protected abstract void ModifyEncapsulatedVariable(IEncapsulateFieldCandidate target, IFieldEncapsulationAttributes attributes, IRewriteSession rewriteSession); //, EncapsulateFieldNewContent newContent)

        protected abstract EncapsulateFieldNewContent LoadNewDeclarationsContent(EncapsulateFieldNewContent newContent, IEnumerable<IEncapsulateFieldCandidate> FlaggedEncapsulationFields);

        protected abstract IList<string> PropertiesContent(IEnumerable<IEncapsulateFieldCandidate> flaggedEncapsulationFields);

        private EncapsulateFieldNewContent LoadNewPropertiesContent(EncapsulateFieldNewContent newContent, IEnumerable<IEncapsulateFieldCandidate> FlaggedEncapsulationFields, string postScript = null)
        {
            if (!FlaggedEncapsulationFields.Any()) { return newContent; }

            var theContent = string.Join($"{Environment.NewLine}{Environment.NewLine}", PropertiesContent(FlaggedEncapsulationFields));
            newContent.AddCodeBlock(theContent);
            if (postScript?.Length > 0)
            {
                newContent.AddCodeBlock($"{postScript}{Environment.NewLine}{Environment.NewLine}");
            }
            return newContent;
        }

        private void RenameReferences(IEncapsulateFieldCandidate efd, string propertyName, IRewriteSession rewriteSession)
        {
            foreach (var reference in efd.Declaration.References)
            {
                var rewriter = rewriteSession.CheckOutModuleRewriter(reference.QualifiedModuleName);
                rewriter.Replace(reference.Context, propertyName ?? efd.Declaration.IdentifierName);
            }
        }
    }
}
