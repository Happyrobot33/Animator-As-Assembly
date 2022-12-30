﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorAsAssemblyFont : MonoBehaviour
{
    //create a font dictionary for a 3x5 font, left to right, top to bottom
    public static Dictionary<char, bool[]> font = new Dictionary<char, bool[]>(){
        {'0', new bool[]{   true, true, true,
                            true, false, true,
                            true, false, true,
                            true, false, true,
                            true, true, true}},
        {'1', new bool[]{   false, true, false,
                            true, true, false,
                            false, true, false,
                            false, true, false,
                            true, true, true}},
        {'2', new bool[]{   true, true, true,
                            false, false, true,
                            true, true, true,
                            true, false, false,
                            true, true, true}},
        {'3', new bool[]{   true, true, true,
                            false, false, true,
                            true, true, true,
                            false, false, true,
                            true, true, true}},
        {'4', new bool[]{   true, false, true,
                            true, false, true,
                            true, true, true,
                            false, false, true,
                            false, false, true}},
        {'5', new bool[]{   true, true, true,
                            true, false, false,
                            true, true, true,
                            false, false, true,
                            true, true, true}},
        {'6', new bool[]{   true, true, true,
                            true, false, false,
                            true, true, true,
                            true, false, true,
                            true, true, true}},
        {'7', new bool[]{   true, true, true,
                            false, false, true,
                            false, false, true,
                            false, false, true,
                            false, false, true}},
        {'8', new bool[]{   true, true, true,
                            true, false, true,
                            true, true, true,
                            true, false, true,
                            true, true, true}},
        {'9', new bool[]{   true, true, true,
                            true, false, true,
                            true, true, true,
                            false, false, true,
                            true, true, true}},
        {'A', new bool[]{   true, true, true,
                            true, false, true,
                            true, true, true,
                            true, false, true,
                            true, false, true}},
        {'B', new bool[]{   true, true, false,
                            true, false, true,
                            true, true, false,
                            true, false, true,
                            true, true, false}},
        {'C', new bool[]{   true, true, true,
                            true, false, false,
                            true, false, false,
                            true, false, false,
                            true, true, true}},
        {'D', new bool[]{   true, true, false,
                            true, false, true,
                            true, false, true,
                            true, false, true,
                            true, true, false}},
        {'E', new bool[]{   true, true, true,
                            true, false, false,
                            true, true, true,
                            true, false, false,
                            true, true, true}},
        {'F', new bool[]{   true, true, true,
                            true, false, false,
                            true, true, true,
                            true, false, false,
                            true, false, false}},
        {'G', new bool[]{   true, true, true,
                            true, false, false,
                            true, false, true,
                            true, false, true,
                            true, true, true}},
        {'H', new bool[]{   true, false, true,
                            true, false, true,
                            true, true, true,
                            true, false, true,
                            true, false, true}},
        {'I', new bool[]{   true, true, true,
                            false, true, false,
                            false, true, false,
                            false, true, false,
                            true, true, true}},
        {'J', new bool[]{   false, false, true,
                            false, false, true,
                            false, false, true,
                            true, false, true,
                            true, true, true}},
        {'K', new bool[]{   true, false, true,
                            true, false, true,
                            true, true, false,
                            true, false, true,
                            true, false, true}},
        {'L', new bool[]{   true, false, false,
                            true, false, false,
                            true, false, false,
                            true, false, false,
                            true, true, true}},
        {'M', new bool[]{   true, false, true,
                            true, true, true,
                            true, true, true,
                            true, false, true,
                            true, false, true}},
        {'N', new bool[]{   true, false, true,
                            true, true, true,
                            true, true, true,
                            true, false, true,
                            true, false, true}},
        {'O', new bool[]{   true, true, true,
                            true, false, true,
                            true, false, true,
                            true, false, true,
                            true, true, true}},
        {'P', new bool[]{   true, true, true,
                            true, false, true,
                            true, true, true,
                            true, false, false,
                            true, false, false}},
        {'Q', new bool[]{   true, true, true,
                            true, false, true,
                            true, false, true,
                            true, true, true,
                            false, false, true}},
        {'R', new bool[]{   true, true, true,
                            true, false, true,
                            true, true, true,
                            true, false, true,
                            true, false, true}},
        {'S', new bool[]{   true, true, true,
                            true, false, false,
                            true, true, true,
                            false, false, true,
                            true, true, true}},
        {'T', new bool[]{   true, true, true,
                            false, true, false,
                            false, true, false,
                            false, true, false,
                            false, true, false}},
        {'U', new bool[]{   true, false, true,
                            true, false, true,
                            true, false, true,
                            true, false, true,
                            true, true, true}},
        {'V', new bool[]{   true, false, true,
                            true, false, true,
                            true, false, true,
                            true, false, true,
                            false, true, false}},
        {'W', new bool[]{   true, false, true,
                            true, false, true,
                            true, false, true,
                            true, true, true,
                            true, true, true}},
        {'X', new bool[]{   true, false, true,
                            true, false, true,
                            false, true, false,
                            true, false, true,
                            true, false, true}},
        {'Y', new bool[]{   true, false, true,
                            true, false, true,
                            false, true, false,
                            false, true, false,
                            false, true, false}},
        {'Z', new bool[]{   true, true, true,
                            false, false, true,
                            false, true, false,
                            true, false, false,
                            true, true, true}},
        {'!', new bool[]{   false, true, false,
                            false, true, false,
                            false, true, false,
                            false, false, false,
                            false, true, false}},
        {'?', new bool[]{   true, true, true,
                            false, false, true,
                            false, true, false,
                            false, false, false,
                            false, true, false}},
        {' ', new bool[]{   false, false, false,
                            false, false, false,
                            false, false, false,
                            false, false, false,
                            false, false, false}},
        {'-', new bool[]{   false, false, false,
                            false, false, false,
                            true, true, true,
                            false, false, false,
                            false, false, false}},
        {'_', new bool[]{   false, false, false,
                            false, false, false,
                            false, false, false,
                            false, false, false,
                            false, false, false,
                            true, true, true}},
        {'=', new bool[]{   false, false, false,
                            false, false, false,
                            true, true, true,
                            false, false, false,
                            true, true, true}},
        {'+', new bool[]{   false, true, false,
                            false, true, false,
                            true, true, true,
                            false, true, false,
                            false, true, false}},
        {'*', new bool[]{   false, true, false,
                            true, false, true,
                            false, true, false,
                            true, false, true,
                            false, true, false}},
        {'/', new bool[]{   false, false, true,
                            false, false, true,
                            false, true, false,
                            true, false, false,
                            true, false, false}},
        {'\\', new bool[]{  true, false, false,
                            true, false, false,
                            false, true, false,
                            false, false, true,
                            false, false, true}},
        {'@', new bool[]{   true, true, true,
                            true, false, true,
                            true, false, true,
                            true, false, false,
                            true, true, true}},
        {'#', new bool[]{   false, true, false,
                            true, true, true,
                            false, true, false,
                            true, true, true,
                            false, true, false}},
        {'$', new bool[]{   false, true, false,
                            true, true, true,
                            false, true, false,
                            true, true, true,
                            false, true, false}},
        {'%', new bool[]{   true, false, true,
                            false, false, true,
                            false, true, false,
                            true, false, false,
                            true, false, true}},
        {'^', new bool[]{   false, true, false,
                            true, false, true,
                            false, false, false,
                            false, false, false,
                            false, false, false}},
        {'&', new bool[]{   false, true, false,
                            true, false, true,
                            false, true, false,
                            true, false, true,
                            false, true, false}},
        {'(', new bool[]{   false, true, false,
                            true, false, false,
                            true, false, false,
                            true, false, false,
                            false, true, false}},
        {')', new bool[]{   false, true, false,
                            false, false, true,
                            false, false, true,
                            false, false, true,
                            false, true, false}},
        {'[', new bool[]{   true, true, false,
                            true, false, false,
                            true, false, false,
                            true, false, false,
                            true, true, false}},
        {']', new bool[]{   false, true, true,
                            false, false, true,
                            false, false, true,
                            false, false, true,
                            false, true, true}},
        {'{', new bool[]{   false, true, false,
                            false, true, false,
                            true, false, false,
                            false, true, false,
                            false, true, false}},
        {'}', new bool[]{   false, true, false,
                            false, true, false,
                            false, false, true,
                            false, true, false,
                            false, true, false}},
        {'<', new bool[]{   false, false, true,
                            false, true, false,
                            true, false, false,
                            false, true, false,
                            false, false, true}},
        {'>', new bool[]{   true, false, false,
                            false, true, false,
                            false, false, true,
                            false, true, false,
                            true, false, false}},
        {'\'', new bool[]{  false, true, false,
                            false, true, false,
                            false, false, false,
                            false, false, false,
                            false, false, false}},
        {'"', new bool[]{   false, true, false,
                            false, true, false,
                            false, true, false,
                            false, false, false,
                            false, false, false}},
        {':', new bool[]{   false, false, false,
                            false, true, false,
                            false, false, false,
                            false, true, false,
                            false, false, false}},
        {';', new bool[]{   false, false, false,
                            false, true, false,
                            false, false, false,
                            false, true, false,
                            false, true, false}},
        {',', new bool[]{   false, false, false,
                            false, false, false,
                            false, false, false,
                            false, true, false,
                            false, true, false}},
        {'.', new bool[]{   false, false, false,
                            false, false, false,
                            false, false, false,
                            false, true, false,
                            false, false, false}},
        {'░', new bool[]{   true, false, true,
                            false, true, false,
                            true, false, true,
                            false, true, false,
                            true, false, true}},
        {'▒', new bool[]{   true, true, true,
                            false, true, false,
                            true, true, true,
                            false, true, false,
                            true, true, true}},
        {'▓', new bool[]{   true, true, true,
                            true, true, true,
                            true, true, true,
                            true, true, true,
                            true, true, true}},
        {'█', new bool[]{   true, true, true,
                            true, true, true,
                            true, true, true,
                            true, true, true,
                            true, true, true}},
        {'▄', new bool[]{   false, false, false,
                            false, false, false,
                            true, true, true,
                            true, true, true,
                            true, true, true}},
        {'▀', new bool[]{   true, true, true,
                            true, true, true,
                            true, true, true,
                            false, false, false,
                            false, false, false}}
    };

    public static Dictionary<char, bool[]> GetFont()
    {
        return font;
    }

    //convert from a char to ascii INT value
    public static int CharToInt(char c)
    {
        return (int)c;
    }

    //convert from ascii INT value to a char
    public static char IntToChar(int i)
    {
        //check if the int is a valid ascii value in our font
        char c = (char)i;
        if (!font.ContainsKey(c))
        {
            //if not, return a ?
            return '?';
        }
        return (char)i;
    }
}
