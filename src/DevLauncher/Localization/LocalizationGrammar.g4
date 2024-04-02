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

fragment UPPERCASE  : [A-Z] ;
fragment LOWERCASE  : [a-z] ;
fragment NUMBER    : [0-9] ;

LANGUAGE			    : 'LANGUAGE' ;

LANG_ID: '\'' [A-Z]* '\'' ;

// String is enclosed inn double-quotes. Allows doubble-double quotes ("") or escaped double-quote (\") inside.
DQSTRING  : '"' (~'"' | '""' | '\\"')* '"';

EQUALS				    : '=' ;
SEMICOLON               : ';';


fragment ID_CHAR : ~['=\r\n\t\u0085\u2028\u2029] ;

IDENTIFIER : ID_CHAR+ ;

fragment NEWLINE: '\r\n' | '\r' | '\n';
fragment WHITESPACE: [ \t];
