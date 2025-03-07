﻿using System.Text;

/// Here we have tokens de-marked by whitespace
/// The use of '=', '+' to do assignment and add.
/// declare and access variables using '$'
/// and a function call where the arguments is the line

var code = @"
$i = 1 + 2 + 3
call Print ""number = "" + $i
call Print ""The End!"" 
gotoif 0 1
".Trim();

// Lexical analysis on the source code:

// the below are some variables to track particular parts of things
var inQuotes = false;
var tokens = new List<string>();
var currentTokenBuilder = new StringBuilder();

// helper method that captures current buffer as a token
void CaptureToken()
{
    if(currentTokenBuilder.Length > 0)
    {
        tokens.Add(currentTokenBuilder.ToString());
        currentTokenBuilder.Clear();
    }
}

// process the code string character by character.
// and using patter-matching as a powerful way to apply rules groups of characters.
// Here 2 chars are processed at once as 2 is enough to infer the intent enough to decide
// when to demark tokens.
for (var ptr = 0; ptr < code.Length; ptr++)
{
    var ch = code[ptr];
    var next = ptr + 1 < code.Length ? code[ptr + 1] : '\0';

    switch (ch, next)
    {
        case ('"', _) when !inQuotes: // start tracking quote
            currentTokenBuilder.Append('"');
            inQuotes = true;
            break;
        case ('"', _) when inQuotes: // stop tracking quote
            currentTokenBuilder.Append('"');
            inQuotes = false;
            break;
        case (_, _ ) when inQuotes: // capture in quotes as a single token
            currentTokenBuilder.Append(ch);
            break;
        case ('\r', _): // ignore return char
        case ('\n', '\n'): // ignore consecutive lines
        case (' ', ' '): // ignore consecutive space
            break;
        case (' ', _): // demark token
            CaptureToken();
            break;
        case ('\n', _): // demark token and capture line end
            CaptureToken();
            tokens.Add("\n");
            break;
        case (_, _ ): // assume part of a token
            currentTokenBuilder.Append(ch);
            break;
    }
}

CaptureToken(); // grab last token
tokens.Add("\n"); // add end statement

Console.WriteLine($"Lexical analysis tokens:");
for(var i = 0; i < tokens.Count; i++)
{
    Console.WriteLine($"{i}: {tokens[i]}");
}

/// Execute the tokens, this is a state machine that walks it's way
/// through the list of tokens and decides what to do for each.
var functionTarget = default(string);
var gotoTarget = default(int?);
var variableTarget = default(string);
var variableMerge = default(object);
var variables = new Dictionary<string, object>();

// handle extracting values from tokens or variables
object GetTokenValue(string token)
{
    var value = default(object);
    if(token[0] == '$') // is var
    {
        value = variables.GetValueOrDefault(token, null);
    }
    else if(token[0] == '"') // is string
    {
        value = token.Trim('"');
    }
    else if(double.TryParse(token, out var number)) // is number
    {
        value = number;
    }   

    return value;
}

for (var ptr = 0; ptr < tokens.Count; ptr++)
{
    var prev = ptr - 1 >= 0 ? tokens[ptr - 1] : null;
    var current = tokens[ptr];
    var next = ptr + 1 < tokens.Count ? tokens[ptr + 1] : null;

    switch (prev, current, next)
    {
        // newline indicates end of a statement
        case (_, "\n", _) when variableTarget != null:
            variables[variableTarget] = variableMerge;
            variableTarget = null;
            break;
        case (_, "\n", _) when functionTarget != null: // run function
            if(functionTarget == "Print")
            {
                Console.WriteLine(variableMerge);
            }
            functionTarget = null;
            break;
        case (_, "\n", _) when gotoTarget != null: // run function
            if(variableMerge is double d && d != 0 )
            {
                ptr = gotoTarget.Value;
            }
            gotoTarget = null;
            break;
        case (_, "+", _): // assume the left side is already the value of variableMerge
            var nextValue = GetTokenValue(next);
            variableMerge = (variableMerge, nextValue) switch
            {
                (null, null) => null,
                (_, null) => variableMerge,
                (null, _) => nextValue,
                (double, string) => variableMerge.ToString() + nextValue,
                (string, double) => variableMerge + nextValue.ToString(),
                (string, string) => (string)variableMerge + (string)nextValue,
                (double, double) => (double)variableMerge + (double)nextValue,
            };
            ptr ++;
            break;
        case (_, "=", _): // assign something, assume the prev is the target and next is merge. eg $i = 1 + 2 + 3
            if(prev[0] == '$')
            {
                variableTarget = prev;
                variableMerge = GetTokenValue(next);;
                ptr ++;
            }
            break;
        case ("call", _, _): // call function
            functionTarget = current;
            variableMerge = GetTokenValue(next);
            ptr ++;
            break;
        case ("gotoif", _, _): // call function
            if(int.TryParse(current, out var addr))
            {
                gotoTarget = addr;
                variableMerge = GetTokenValue(next);
                ptr ++;
            }
            break;
        case (_, _, _) when variableTarget != null: 
            break;
    }
}

Console.WriteLine("");