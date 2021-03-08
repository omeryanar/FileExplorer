using System;
using System.Linq;
using System.Text.RegularExpressions;
using DevExpress.Data.Filtering;

namespace FileExplorer.Core
{
    public abstract class StringFunction : ICustomFunctionOperatorBrowsable
    {
        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract int MinOperandCount { get; }

        public abstract int MaxOperandCount{ get; }

        public abstract bool IsValidOperandCount(int count);

        public abstract bool IsValidOperandType(int operandIndex, int operandCount, Type type);

        public abstract object Evaluate(params object[] operands);

        public FunctionCategory Category => FunctionCategory.Text;

        public Type ResultType(params Type[] operands)
        {
            return typeof(string);
        }
    }

    public abstract class SingleArgumentStringFunction : StringFunction
    {
        public override int MinOperandCount => 1;

        public override int MaxOperandCount => 1;

        public override bool IsValidOperandCount(int count)
        {
            return count == 1;
        }

        public override bool IsValidOperandType(int operandIndex, int operandCount, Type type)
        {
            return operandCount == 1;
        }
    }

    public class ToggleCaseFunction : SingleArgumentStringFunction
    {
        public override string Name => "ToggleCase";

        public override string Description => "ToggleCase(String) Returns String by converting upper case to lower case, lower case to upper case.";

        public override object Evaluate(params object[] operands)
        {
            return operands[0]?.ToString().ToggleCase();
        }
    }

    public class TitleCaseFunction : SingleArgumentStringFunction
    {
        public override string Name => "TitleCase";

        public override string Description => "TitleCase(String) Returns String by capitalizing the first letter of each word and leave the other letters lowercase.";

        public override object Evaluate(params object[] operands)
        {
            return operands[0]?.ToString().TitleCase();
        }
    }

    public class SentenceCaseFunction : SingleArgumentStringFunction
    {
        public override string Name => "SentenceCase";

        public override string Description => "SentenceCase(String) Returns String by capitalizing the first letter and leave all other letters as lowercase.";

        public override object Evaluate(params object[] operands)
        {
            return operands[0]?.ToString().SentenceCase();
        }
    }

    public class RemoveInvalidFileNameCharactersFunction : SingleArgumentStringFunction
    {
        public override string Name => "RemoveInvalidFileNameCharacters";

        public override string Description => @"RemoveInvalidFileNameCharacters(String) Returns String by removing characters that are not allowed in file names: \ / : * ? "" < > |";

        public override object Evaluate(params object[] operands)
        {
            return operands[0]?.ToString().RemoveInvalidFileNameCharacters();
        }
    }

    public abstract class DoubleArgumentStringFunction : StringFunction
    {
        public override int MinOperandCount => 2;

        public override int MaxOperandCount => 2;

        public override bool IsValidOperandCount(int count)
        {
            return count == 2;
        }

        public override bool IsValidOperandType(int operandIndex, int operandCount, Type type)
        {
            return operandCount == 2;
        }
    }

    public class RegexMatchFunction : DoubleArgumentStringFunction
    {
        public override string Name => "RegexMatch";

        public override string Description => "RegexMatch(String, expression) Searches the specified String for the first occurrence of the specified regular expression.";

        public override object Evaluate(params object[] operands)
        {
            return Regex.Match(operands[0]?.ToString(), operands[1]?.ToString()).Value;
        }
    }

    public class RegexIsMatchFunction : DoubleArgumentStringFunction
    {
        public override string Name => "RegexIsMatch";

        public override string Description => "RegexIsMatch(String, expression) Returns True if the specified regular expression finds a match in the specified String; otherwise, False is returned.";

        public override object Evaluate(params object[] operands)
        {
            return Regex.IsMatch(operands[0]?.ToString(), operands[1]?.ToString());
        }
    }

    public abstract class TripleArgumentStringFunction : StringFunction
    {
        public override int MinOperandCount => 3;

        public override int MaxOperandCount => 3;

        public override bool IsValidOperandCount(int count)
        {
            return count == 3;
        }

        public override bool IsValidOperandType(int operandIndex, int operandCount, Type type)
        {
            return operandCount == 3;
        }
    }

    public class RegexConcatFunction : TripleArgumentStringFunction
    {
        public override string Name => "RegexConcat";

        public override string Description => "RegexConcat(String, expression, separator) Returns String by concatenating all strings that match the specified regular expression using the specified separator.";

        public override object Evaluate(params object[] operands)
        {
            return String.Join(operands[2]?.ToString(), Regex.Matches(operands[0]?.ToString(), operands[1]?.ToString()).Cast<Match>().Select(x => x.Value));
        }
    }

    public class RegexReplaceFunction : TripleArgumentStringFunction
    {
        public override string Name => "RegexReplace";

        public override string Description => "RegexReplace(String, expression, replacement) Returns String by replacing all strings that match the specified regular expression with the specified replacement.";

        public override object Evaluate(params object[] operands)
        {
            return Regex.Replace(operands[0]?.ToString(), operands[1]?.ToString(), operands[2]?.ToString());
        }
    }

    public abstract class VariableArgumentStringFunction : StringFunction
    {
        public override int MinOperandCount => 1;

        public override int MaxOperandCount => 1;

        public override bool IsValidOperandCount(int count)
        {
            return count > 0;
        }

        public override bool IsValidOperandType(int operandIndex, int operandCount, Type type)
        {
            return operandCount > 0;
        }
    }

    public class StringFormatFunction : VariableArgumentStringFunction
    {
        public override string Name => "StringFormat";

        public override string Description => "StringFormat(format, arguments) replaces the format item in a specified string with the string representation of a corresponding object in a specified array";

        public override object Evaluate(params object[] operands)
        {
            return String.Format(operands[0]?.ToString(), operands.Skip(1).ToArray());
        }
    }
}
