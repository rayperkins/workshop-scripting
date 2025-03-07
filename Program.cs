using System.Text;

/// Here we have tokens de-marked by whitespace
/// The use of '=', '+' to do assignment and add.
/// declare and access variables using '$'
/// and a function call where the arguments is the line

var code = @"
$i = 1 + 2
call Print ""number = "" + $i
".Trim();

// Lexical analysis on the source code:

// the below are some variables to track particular parts of things
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