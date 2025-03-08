using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using FrontierQuotes.Models;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.Input;

namespace FrontierQuotes.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    [ObservableProperty]
    public partial string[] Names { get; set; }

    [ObservableProperty]
    public partial string? SelectedName { get; set; }
    
    [ObservableProperty]
    public partial string? NameInput { get; set; }

    public ObservableCollection<Bitmap> Sprites = new();

    [ObservableProperty]
    public partial Bitmap? SelectedSprite { get; set; }

    [ObservableProperty]
    public partial string? SelectedQuote { get; set; }

    private readonly List<string> _loadedSprites = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StreakText))]
    public partial uint Streak { get; set; }
    
    [ObservableProperty]
    public partial bool IncludeWinning { get; set; }
    
    [ObservableProperty]
    public partial bool IncludeLosing { get; set; }
    
    [ObservableProperty]
    public partial bool AnswerVisible { get; set; }
    
    [ObservableProperty]
    public partial string? AnswerText {get; set;}

    [ObservableProperty]
    public partial bool IncludeGen3 { get; set; } = true;

    [ObservableProperty]
    public partial bool IncludeGen4 { get; set; } = true;
    
    public string StreakText => $"Streak: {Streak}";
    
    private List<TrainerQuoted> _trainerQuoteds;
    
    private TrainerQuoted? _correctTrainerQuoted;

    private string[] _allNames;


    private const int Gen4TrainerCount = 300;
    private int _startIdx = 0;
    private int _endIdx = 0;


    public MainViewModel()
    {
        List<TrainerQuoted>? gen4 = LoadInfo(new Uri("avares://FrontierQuotes/Assets/gen4/trainers.json"))!;
        List<TrainerQuoted>? gen3 = LoadInfo(new Uri("avares://FrontierQuotes/Assets/gen3/trainers.json"))!;
        _trainerQuoteds = gen4;
        _trainerQuoteds.AddRange(gen3);
        int ii = -1;
        foreach (TrainerQuoted trainer in _trainerQuoteds!)
        {
            ii++;
            if (_loadedSprites.Contains(trainer.Sprite))
            {
                continue;
            }
            Uri uri = ii < Gen4TrainerCount ? new Uri($"avares://FrontierQuotes/Assets/gen4/Sprites/{trainer.Sprite}") : new Uri($"avares://FrontierQuotes/Assets/gen3/Sprites/{trainer.Sprite}");
            Sprites.Add(new Bitmap(AssetLoader.Open(uri)));
            _loadedSprites.Add(trainer.Sprite);
        }
        var allNames = _trainerQuoteds.Select(s => s.Name); 
        List<string> names = new();
        foreach(var name in allNames)
        {
            // check if name is the start of another name
            if (allNames.Any(n => n.StartsWith(name) && n != name))
            {
                names.Add($"{name}.");
            }
            else
            {
                names.Add(name);
            }
        }
        _allNames = [.. names];
        Names = [.. names];
        _endIdx = Names.Length;
        _ = NextQuote();
    }
    
    partial void OnSelectedNameChanged(string? value)
    {
        if(value is null) return;
        if (_correctTrainerQuoted?.Name == value)
        {
            Streak++;
        }
        else
        {
            Streak = 0;
        }
        _ = NextQuote(true);
    }

    partial void OnIncludeLosingChanged(bool value)
    {
        SettingsChanged();
    }

    partial void OnIncludeWinningChanged(bool value)
    {
        SettingsChanged();
    }

    private void SettingsChanged()
    {
        Streak = 0;
        _ = NextQuote();
    }

    partial void OnIncludeGen3Changed(bool oldValue, bool newValue)
    {
        GenChanged();
    }

    partial void OnIncludeGen4Changed(bool oldValue, bool newValue)
    {
        GenChanged();
    }

    private void GenChanged()
    {
        if (IncludeGen4 && !IncludeGen3)
        {
            _startIdx = 0;
            _endIdx = Gen4TrainerCount;
            Names = _allNames[.._endIdx];
        }
        else if (IncludeGen3 && !IncludeGen4)
        {
            _startIdx = Gen4TrainerCount;
            _endIdx = Names.Length;
            Names = _allNames[_startIdx..];
        }
        else
        {
            // Empty or both, you get them all
            _startIdx = 0;
            _endIdx = Names.Length;
            Names = _allNames;
        }
        Streak = 0;
        _ = NextQuote();
    }

    private List<TrainerQuoted>? LoadInfo(Uri uri)
    {
        using Stream stream = AssetLoader.Open(uri);
        using StreamReader reader = new(stream);
        string json =  reader.ReadToEnd();
        return JsonSerializer.Deserialize<List<TrainerQuoted>>(json, SourceGenerationContext.Default.ListTrainerQuoted);
    }

    private async Task NextQuote(bool nextAnswer = false)
    {
        
        // TODO: Look into sorting this "properly"
        await Task.Delay(TimeSpan.FromSeconds(0.01));
        SelectedName = null;
        await Task.Delay(TimeSpan.FromSeconds(0.01));
        NameInput = string.Empty;
        
        if (nextAnswer && _correctTrainerQuoted != null)
        {
            AnswerText = $"- {_correctTrainerQuoted.Class} {_correctTrainerQuoted.Name}";
            AnswerVisible = true;
            await Task.Delay(TimeSpan.FromSeconds(2));
            AnswerVisible = false;
        }
        
        int trainerId = RandomNumberGenerator.GetInt32(_startIdx, _endIdx);
        int spriteId = _loadedSprites.FindIndex(s => s.Equals(_trainerQuoteds[trainerId].Sprite));
        SelectedSprite = Sprites[spriteId];
        _correctTrainerQuoted = _trainerQuoteds[trainerId];
        
        // TODO: Revisit this logic...
        List<string> choices = ["Greeting"];
        if (IncludeWinning)
        {
            choices.Add("Win");
        }

        if (IncludeLosing)
        {
            choices.Add("Loss");
        }
        
        string quoteType = choices[RandomNumberGenerator.GetInt32(0, choices.Count)];
        SelectedQuote = quoteType switch
        {
            "Greeting" => _trainerQuoteds[trainerId].Greeting,
            "Win" => _trainerQuoteds[trainerId].Win,
            "Loss" => _trainerQuoteds[trainerId].Loss,
            _ => _trainerQuoteds[trainerId].Greeting
        };
        
    }

    [RelayCommand]
    public async Task Skip()
    {
        Streak = 0;
        await NextQuote(true);
    }
    
}
