namespace TinyCompilerForTinyBasic;

public class TinyBasicToken
{
    public TinyBasicToken(){}
    public TinyBasicToken(TBTokenType type) => Type = type;
    
    public TBTokenType Type { get; init; } = TBTokenType.Unknown;
}

public class ValueTinyBasicTinyBasicToken : TinyBasicToken
{
    public ValueTinyBasicTinyBasicToken(){}
    public ValueTinyBasicTinyBasicToken(TBTokenType type, string value) : base(type) => Value = value;
    
    public string Value { get; init; } = string.Empty;
}

public enum TBTokenType
{
    Unknown,
    Comma,
    NewLine,
    Number,
    String,
    QuotedString,
    ParenthesisOpen,
    ParenthesisClose,
    OperatorPlus,
    OperatorMinus,
    OperatorMultiplication,
    OperatorDivision,
    OperatorEquals,
    OperatorNotEqual,
    OperatorGreaterThan,
    OperatorGreaterThanOrEqual,
    OperatorLessThan,
    OperatorLessThanOrEqual,
}