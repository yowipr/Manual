using CommunityToolkit.Mvvm.ComponentModel;
using Manual.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Manual.Objects;

public static class SentenceTagger
{


    [XmlIgnore] public static List<TagType> TagTypes = new();

    public static TagType GetTagType(string sentence)
    {
        string[] words = sentence.Split(' ');
        for (int i = words.Length - 1; i >= 0; i--)
        {
            string word = words[i];
            foreach (var tagType in TagTypes)
            {
                if (tagType.Words != null && tagType.Words.Contains(word))
                {
                    return tagType;
                }
            }
        }

        return TagType.Unknown;
    }

    public static TagType GetTagType(ObservableCollection<PromptTag> promptTag)
    {
        return GetTagType(ConvertToString(promptTag));
    }


    public static void SaveTagTypes()
    {
        string filePath = $"{App.LocalPath}Resources/tagtypes";

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var tagType in TagTypes)
            {
                writer.WriteLine($"{tagType.NameType},{tagType.ColorType.ToHex()}:");

                if (tagType.Words != null)
                {
                    foreach (var word in tagType.Words)
                    {
                        writer.WriteLine(word);
                    }
                }

                writer.WriteLine();
            }
        }
    }

    public static void LoadTagTypes()
    {
        string filePath = $"{App.LocalPath}Resources/tagtypes";

        TagTypes.Clear();

        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            TagType currentTagType = null;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.Contains(","))
                {
                    string[] parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        string name = parts[0];
                        string colorHex = parts[1].Replace(":", "");
                        currentTagType = new TagType(name, colorHex.ToColor());
                        TagTypes.Add(currentTagType);
                    }
                }
                else if (currentTagType != null && !string.IsNullOrWhiteSpace(line))
                {
                    currentTagType.Words ??= new List<string>();
                    currentTagType.Words.Add(line);
                }
            }
        }




    }



    public static ObservableCollection<PromptTag> ConvertToPromptTags(string prompt, bool reverse = false)
    {
        string[] prompts = prompt.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim())
                         .ToArray();

        List<PromptTag> list = new();
        foreach (string p in prompts)
        {
            PromptTag promptTag = new PromptTag(p);
            list.Add(promptTag);
        }

        if (reverse == true)
            list.Reverse();

        return new ObservableCollection<PromptTag>(list);
    }

    public static string ConvertToString(IEnumerable<PromptTag> promptTags)
    {
        promptTags = new ObservableCollection<PromptTag>(promptTags.Reverse());
        return string.Join(", ", promptTags.Select(tag => tag.Prompt));
    }



}

public class TagType
{
    public string NameType = "unknown";
    public  Color ColorType = Colors.Yellow;
    public List<string> Words;

    public TagType()
    {
        
    }
    public TagType(string name, Color color)
    {
        NameType = name;
        ColorType = color;
    }
    public TagType(string name, string colorHex)
    {
        NameType = name;
        ColorType = colorHex.ToColor();
    }


    public override string ToString()
    {
        return NameType;
    }


    public static TagType Unknown = new TagType("unknown", "#202020".ToColor() );
}


public partial class PromptTag : ObservableObject
{
    [ObservableProperty] string prompt = "";
    [ObservableProperty] Color colorType = Colors.Gray;
    [ObservableProperty] TagType tag;
    [ObservableProperty] string tagName = "unknown";

    public PromptTag()


    {

    }
    public PromptTag(string prompt)
    {
        Prompt = prompt;
        Tag = SentenceTagger.GetTagType(prompt);
        ColorType = Tag.ColorType;
        TagName = tag.NameType;
    }

    [ObservableProperty] SolidColorBrush colorTypeBrush;

    partial void OnColorTypeChanged(Color value)
    {
        ColorTypeBrush = new SolidColorBrush(value);
    }
}

public partial class GroupTag : PromptTag
{
    [ObservableProperty] string name = "Group";
    [ObservableProperty] ObservableCollection<PromptTag> promptTags = new();
    [ObservableProperty] bool isExpanded = false;

    public GroupTag()
    {
        
    }
    public GroupTag(string prompt) : base(prompt)
    {
        Prompt = prompt;
        Tag = SentenceTagger.GetTagType(prompt);
        ColorType = Tag.ColorType;
        Name = Tag.NameType;
        PromptTags = SentenceTagger.ConvertToPromptTags(prompt);
    }

    public GroupTag(ObservableCollection<PromptTag> promptTag)
    {
        PromptTags = promptTag;
        Prompt = SentenceTagger.ConvertToString(promptTag);
        Tag = SentenceTagger.GetTagType(promptTag);
        ColorType = Tag.ColorType;
        Name = Tag.NameType; 
    }

     partial void OnPromptTagsChanged(ObservableCollection<PromptTag> value)
      {
          Prompt = SentenceTagger.ConvertToString(value);
      }


}


