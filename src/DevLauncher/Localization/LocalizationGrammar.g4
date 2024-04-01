grammar LocalizationGrammar;


/*
 * Parser Rules
 */
localizationFile	:  languageSpec entryList? EOF ;
languageSpec		:  LANGUAGE	EQUALS language SEMICOLON ;
language            :  LANG_ID ;
entryList			:  entry+ ;
entry				:  key EQUALS value ;
key                 :  IDENTIFIER ;
value               :  ENTRY_VALUE;


/*
 * Lexer Rules
 */
 
 WHITESPACES
     : (WHITESPACE | NEWLINE)+
     -> channel(HIDDEN);

fragment SimpleEscapeSequence:
    '\\\''
    | '\\"'
    | '\\\\'
    | '\\n'
    | '\\r'
    | '\\t'
;
    
fragment UPPERCASE  : [A-Z] ;
fragment LOWERCASE  : [a-z] ;
fragment NUMBER    : [0-9] ;

fragment EXTENDED_ID_CHAR    : [\u0080-\u00FF] ;

EQUALS				    : '=' ;
SEMICOLON               : ';';
LANGUAGE			    : 'LANGUAGE' ;


ID_CHAR : (UPPERCASE | LOWERCASE | NUMBER | ' ' | '.' | '_' | '-') ;
IDENTIFIER : ID_CHAR+ ;

ENTRY_VALUE :  IDENTIFIER | DQSTRING;
DQSTRING : '"' (~["\\\r\n\u0085\u2028\u2029] | SimpleEscapeSequence)* '"' ;

LANG_ID: '\'' [A-Z]* '\'' ;

fragment NEWLINE: '\r\n' | '\r' | '\n';
fragment WHITESPACE: [ \t];
