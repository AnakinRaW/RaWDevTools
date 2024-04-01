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
value               :  IDENTIFIER | DQSTRING;


/*
 * Lexer Rules
 */
 
 LINE_COMMENT
     : '#' InputCharacter*
     -> channel(HIDDEN);
 
 WHITESPACES
     : (WHITESPACE | NEWLINE)+
     -> channel(HIDDEN);
     
fragment InputCharacter: ~[\r\n]; // Anything but NewLine

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

fragment ID_CHAR : ~['=\r\n\t\u0085\u2028\u2029] ;

IDENTIFIER : ID_CHAR+ ;

LANG_ID: '\'' [A-Z]* '\'' ;

fragment NEWLINE: '\r\n' | '\r' | '\n';
fragment WHITESPACE: [ \t];

DQSTRING  : '"' (~'"' | ~[\t] | '""')* '"';
