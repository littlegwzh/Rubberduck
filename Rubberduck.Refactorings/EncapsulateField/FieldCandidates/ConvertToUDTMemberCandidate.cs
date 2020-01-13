﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Rubberduck.Common;
using Rubberduck.Parsing.Grammar;
using Rubberduck.Parsing.Symbols;
using Rubberduck.VBEditor;

namespace Rubberduck.Refactorings.EncapsulateField
{

    public interface IConvertToUDTMember : IEncapsulateFieldCandidate
    {
        string UDTMemberDeclaration { get; }
        IEncapsulateFieldCandidate WrappedCandidate { get; }
    }

    public class ConvertToUDTMember : IConvertToUDTMember
    {
        private int _hashCode;
        private readonly string _uniqueID;
        private readonly IEncapsulateFieldCandidate _wrapped;
        public ConvertToUDTMember(IEncapsulateFieldCandidate candidate, IObjectStateUDT objStateUDT)
        {
            _wrapped = candidate;
            PropertyIdentifier = _wrapped.PropertyIdentifier;
            ObjectStateUDT = objStateUDT;
            _uniqueID = BuildUniqueID(candidate, objStateUDT);
            _hashCode = _uniqueID.GetHashCode();
        }

        public virtual string UDTMemberDeclaration
        {
            get
            {
                if (_wrapped is IArrayCandidate array)
                {
                   return array.UDTMemberDeclaration;
                }
                return $"{BackingIdentifier} As {_wrapped.AsTypeName}";
            }
        }

        public IEncapsulateFieldCandidate WrappedCandidate => _wrapped;

        public IObjectStateUDT ObjectStateUDT { private set; get; }

        public string TargetID => _wrapped.TargetID;

        public Declaration Declaration => _wrapped.Declaration;

        public bool EncapsulateFlag
        {
            set => _wrapped.EncapsulateFlag = value;
            get => _wrapped.EncapsulateFlag;
        }

        public string PropertyIdentifier
        {
            set => _wrapped.PropertyIdentifier = value;
            get => _wrapped.PropertyIdentifier;
        }

        public string PropertyAsTypeName => _wrapped.PropertyAsTypeName;

        public string BackingIdentifier
        {
            set { }
            get => PropertyIdentifier;
        }
        public string BackingAsTypeName => Declaration.AsTypeName;

        public bool CanBeReadWrite
        {
            set => _wrapped.CanBeReadWrite = value;
            get => _wrapped.CanBeReadWrite;
        }

        public bool ImplementLet => _wrapped.ImplementLet;

        public bool ImplementSet => _wrapped.ImplementSet;

        public bool IsReadOnly
        {
            set => _wrapped.IsReadOnly = value;
            get => _wrapped.IsReadOnly;
        }

        public string ParameterName
        {
            set => _wrapped.ParameterName = value;
            get => _wrapped.ParameterName;
        }

        public IValidateVBAIdentifiers NameValidator
        {
            set => _wrapped.NameValidator = value;
            get => _wrapped.NameValidator;
        }

        public IEncapsulateFieldConflictFinder ConflictFinder
        {
            set => _wrapped.ConflictFinder = value;
            get => _wrapped.ConflictFinder;
        }

        private string AccessorInProperty
        {
            get
            {
                if (_wrapped is IUserDefinedTypeMemberCandidate udtm)
                {
                    return $"{ObjectStateUDT.FieldIdentifier}.{udtm.UDTField.PropertyIdentifier}.{BackingIdentifier}";
                }
                return $"{ObjectStateUDT.FieldIdentifier}.{BackingIdentifier}";
            }
        }

        public string ReferenceAccessor(IdentifierReference idRef)
        {
            if (idRef.QualifiedModuleName != QualifiedModuleName)
            {
                return PropertyIdentifier;
            }
            return  BackingIdentifier;
        }

        public string IdentifierName => _wrapped.IdentifierName;

        public QualifiedModuleName QualifiedModuleName => _wrapped.QualifiedModuleName;

        public string AsTypeName => _wrapped.AsTypeName;

        public bool TryValidateEncapsulationAttributes(out string errorMessage)
        {
            return ConflictFinder.TryValidateEncapsulationAttributes(this, out errorMessage);
        }

        public IEnumerable<PropertyAttributeSet> PropertyAttributeSets
        {
            get
            {
                var modifiedSets = new List<PropertyAttributeSet>();
                var sets = _wrapped.PropertyAttributeSets;
                for (var idx = 0; idx < sets.Count(); idx++)
                {
                    var attributeSet = sets.ElementAt(idx);
                    var fields = attributeSet.BackingField.Split(new char[] { '.' });

                    attributeSet.BackingField = fields.Count() > 1
                        ? $"{ObjectStateUDT.FieldIdentifier}.{attributeSet.BackingField.CapitalizeFirstLetter()}"
                        : $"{ObjectStateUDT.FieldIdentifier}.{attributeSet.PropertyName.CapitalizeFirstLetter()}";

                    modifiedSets.Add(attributeSet);
                }
                return modifiedSets;
            }
        }

        public override bool Equals(object obj)
        {
            return obj != null
                && obj is ConvertToUDTMember convertWrapper
                && BuildUniqueID(convertWrapper, convertWrapper.ObjectStateUDT) == _uniqueID;
        }

        public override int GetHashCode() => _hashCode;

        private static string BuildUniqueID(IEncapsulateFieldCandidate candidate, IObjectStateUDT field) => $"{candidate.QualifiedModuleName.Name}.{field.IdentifierName}.{candidate.IdentifierName}";

        private PropertyAttributeSet CreateMemberPropertyAttributeSet (IUserDefinedTypeMemberCandidate udtMember)
        {
            return new PropertyAttributeSet()
            {
                PropertyName = udtMember.PropertyIdentifier,
                BackingField = $"{ObjectStateUDT.FieldIdentifier}.{udtMember.UDTField.PropertyIdentifier}.{udtMember.BackingIdentifier}",
                AsTypeName = udtMember.PropertyAsTypeName,
                ParameterName = udtMember.ParameterName,
                GenerateLetter = udtMember.ImplementLet,
                GenerateSetter = udtMember.ImplementSet,
                UsesSetAssignment = udtMember.Declaration.IsObject,
                IsUDTProperty = (udtMember.Declaration.AsTypeDeclaration?.DeclarationType ?? DeclarationType.Variable) == DeclarationType.UserDefinedType
            };
        }

        private PropertyAttributeSet AsPropertyAttributeSet
        {
            get
            {
                return new PropertyAttributeSet()
                {
                    PropertyName = PropertyIdentifier,
                    BackingField = AccessorInProperty,
                    AsTypeName = PropertyAsTypeName,
                    ParameterName = ParameterName,
                    GenerateLetter = ImplementLet,
                    GenerateSetter = ImplementSet,
                    UsesSetAssignment = Declaration.IsObject,
                    IsUDTProperty = true
                };
            }
        }
    }
}
