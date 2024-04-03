grammar LocalizationGrammar;

// Parser Rules

localizationFile	:  languageSpec entryList? EOF ;
languageSpec		:  LANGUAGE	EQUALS language SEMICOLON ;
language            :  LANG_ID ;
entryList			:  entry+ ;
entry				:  key EQUALS value ;
key                 :  IDENTIFIER ;
value               :  IDENTIFIER | VALUE | DQSTRING;


// Lexer Rules

LINE_COMMENT
    : '#' InputCharacter*
    -> channel(HIDDEN);

fragment InputCharacter : ~[\r\n]; // Anything but NewLine

fragment UPPERCASE	: [A-Z] ;
fragment LOWERCASE	: [a-z] ;
fragment NUMBER		: [0-9] ;
fragment NEWLINE	: '\r\n' | '\r' | '\n';
fragment WHITESPACE	: [ \t];

LANGUAGE			    : 'LANGUAGE' ;

EQUALS		: '=' ;
SEMICOLON	: ';';

fragment ID_CHAR: 
    UPPERCASE 
    | LOWERCASE
    | NUMBER
    | '_'
    | '-'
    | ' '
    | '.'
    ;
fragment VALUE_CHAR: 
    ~['=\r\n\t\u0085\u2028\u2029]
    ;
fragment SimpleEscapeSequence:
    '\\\''
    | '\\"'
    | '\\='
    | '\\n'
    | '\\r'
    ;
    
// String is enclosed inn double-quotes. Allows doubble-double quotes ("") or escaped double-quote (\") inside.
DQSTRING	: '"' (~'"' | '""' | '\\"')* '"';
LANG_ID		: '\'' UPPERCASE+ '\'' ;
    
IDENTIFIER	: ID_CHAR+ ;
VALUE		: (ID_CHAR | VALUE_CHAR | SimpleEscapeSequence)+; 
 
 WHITESPACES
    : (WHITESPACE | NEWLINE)+
    -> channel(HIDDEN);