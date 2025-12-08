using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RinCore
{
    public static partial class BadWords
    {
        static List<string> cute1 = new()
        {
            "Fluffy","Sparkly","Bouncy","Tiny","Happy","Snuggle","Cuddly","Giggly","Puffy","Fuzzy",
            "Twinkle","Wiggly","Jolly","Doodle","Sunny","Shiny","Peppy","Mushy","Squeaky","Cheery",
            "Merry","Snazzy","Glimmer","Bubbly","Winky","Perky","Sprinkle","Lovey","Huggy","Twirly",
            "Frothy","Nibble","Puddle","Gleamy","Chirpy","Tappy","Snoozy","Wobbly","Kissy","Flippy",
            "Poppy","Snippy","Dizzy","Crispy","Hoppy","Sparky","Gooey","Twisty","Flicky","Bouncy",
            "Snuffy","Peachy","Chummy","Jumpy","Plucky","Squishy","Wiggly","Zippy","Mushy","Dandy",
            "Nifty","Giddy","Shushy","Tappy","Pearly","Whimsy","Cutesy","Blinky","Froggy","Snazzy",
            "Jingly","Wiggly","Lovable","Swoony","Tickly","Chirpy","Flippy","Bubbly","Nuzzle","Twinkly",
            "Hugsy","Mimsy","Sprightly","Snicker","Fizzy","Bouncy","Puddle","Giggly","Fuzzy","Perky",
            "Snappy","Doodle","Wiggly","Merry","Fluffy","Poppy","Twisty","Snuggy","Gleeful","Pearly",
            "Blinky","Sparky","Cuddly","Twinkly","Snazzy","Wobbly","Chummy","Glimmer","Lovey","Peppy"
        };

        static List<string> cute2 = new()
        {
            "Bun","Bean","Puff","Cup","Muffin","Paw","Button","Pie","Sprout","Nugget",
            "Biscuit","Pebble","Pumpkin","Toffee","Pop","Cookie","Teddy","Cherry","Daisy","Snickerdoodle",
            "Fawn","Pudding","Poppy","Marshmallow","Kitty","Peanut","Snugglebug","Honey","Buttercup","Clover",
            "Bunny","Coco","Noodle","Peaches","Sugar","Twinkle","Mochi","Snick","Pumpkinseed","Pipsqueak",
            "Cupcake","Bubble","Pudding","Fritter","Sprinkles","Chick","Beanie","Muffin","Peapod","Waffle",
            "Cuddlepuff","Snickers","Fizzy","Dumpling","Gingersnap","Snugglepie","Cuddlepaw","Tootsie","Poppyseed","Tinker",
            "Honeybun","Button","Lollipop","Peach","Marzipan","Nugget","Muffin","Puddingpop","Snuzzle","Cherryblossom",
            "Teddybear","Cinnamon","Puffball","Twinkie","Nibbles","Sprout","Cookie","Biscuit","Cupcake","Mochi",
            "Snuggles","Peanutbutter","Wiggles","Froggie","Cuddlekins","Sugarplum","Noodle","Puddle","Snicker","Bumble",
            "Churro","Snazzybun","Twixie","Marsh","Taffy","Jellybean","Doodlebug","Muffinpuff","Honeydrop","Pipsy",
            "Cuppycake","Wiggle","Snookums","Snuggaboo","Bubbles","Fritters","Peanutty","Sprinkles","Tootsie","Snappy"
        };

        static List<string> cute3 = new()
        {
            "Face","Tail","Ears","Nose","Toes","Feet","Hands","Paws","Snout","Cheeks",
            "Whiskers","Fur","Bloom","Blossom","Petal","Sprig","Bud","Twig","Leaf","Berry",
            "Fluff","Fang","Wing","Horn","Claw","Bean","Snout","Button","Spot","Dot",
            "Heart","Star","Moon","Sun","Cloud","Raindrop","Bubble","Pebble","Stone","Rock",
            "Leaflet","Twiglet","Sprout","Seed","Budlet","Petalette","Puff","Pop","Doodle","Muff",
            "Tailsie","Earbud","Snuggletoes","Fuzzyface","Gigglepuff","Hugface","Snuzzlepaw","Chirp","Twinkletoe","Wiggletail",
            "Bunnyface","Kittenpaw","Puppytoe","Puddleface","Marshmallowtail","Cupcaketoe","Sproutpaw","Noodleface","Snickerpaw","Doodletoe",
            "Pebbleface","Cherrytoe","Sugarpaw","Butterface","Honeytoe","Biscuitpaw","Muffinface","Pipsytail","Cuddlypaw","Twinklepuff",
            "Fizzyface","Snappytoe","Wigglytail","Bubblyface","Snuggletoe","Mochipuff","Peanutpaw","Fritterface","Sprinkletoe","Nuggettail",
            "Cuppyface","Buttonpaw","Tootsietoe","Poppyface","Jellypaw","Doodleface","Fuzzytoe","Twinkletail","Snazzyface","Bunnytoe"
        };
        public static string CleanNameGen(int characterLimit, params List<string>[] nameLists)
        {
            int shortestNameLength()
            {
                int length = 0;
                foreach (var list in nameLists)
                {
                    int shortest = int.MaxValue;
                    foreach (string str in list)
                    {
                        if (str.Length < shortest)
                            shortest = str.Length;
                    }
                    length += shortest;
                }
                return length;
            }

            int min = shortestNameLength();
            characterLimit = Math.Max(min, characterLimit);

            string generatedName = "";
            int attemptsRemaining = 50;

            while ((string.IsNullOrEmpty(generatedName) || generatedName.Length > characterLimit) && attemptsRemaining > 0)
            {
                attemptsRemaining--;
                generatedName = "";
                foreach (var list in nameLists)
                {
                    if (list.Count == 0) continue;
                    string part = list[UnityEngine.Random.Range(0, list.Count)].Capitalized();
                    generatedName += part;
                }
            }

            generatedName = generatedName.ClampLength(characterLimit);
            return generatedName.SafeString(preserveCapitals: true, removeSpaces: true, preserveUnderscore: false);
        }
        public static bool CleanReplaceFunny(string input, HashSet<string> badWords, out string clean, int characterLimit = 12)
        {
            clean = input;
            if (string.IsNullOrWhiteSpace(input) || badWords == null || badWords.Count == 0)
                return false;
            string normalizedInput = input.SafeString(preserveCapitals: false, removeSpaces: true, preserveUnderscore: true);
            foreach (var badWord in badWords)
            {
                if (normalizedInput.Contains(badWord, StringComparison.OrdinalIgnoreCase))
                {
                    clean = Helper.SeededRandomInt256 > 150 ? CleanNameGen(characterLimit, cute1, cute2, cute3) : CleanNameGen(characterLimit, cute1, cute2);
                    return true;
                }
            }
            return false;
        }
    }
}