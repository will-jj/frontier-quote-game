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
    
    public string StreakText => $"Streak: {Streak}";
    
    private List<TrainerQuoted> _trainerQuoteds;
    
    private TrainerQuoted? _correctTrainerQuoted;
    
    public MainViewModel()
    {
        _trainerQuoteds = LoadInfo()!;
        foreach (TrainerQuoted trainer in _trainerQuoteds!)
        {
            if (_loadedSprites.Contains(trainer.Sprite))
            {
                continue;
            }
            Sprites.Add(new Bitmap(AssetLoader.Open(new Uri($"avares://FrontierQuotes/Assets/Sprites/{trainer.Sprite}"))));
            _loadedSprites.Add(trainer.Sprite);
        }
        Names = _trainerQuoteds.Select(s => s.Name).ToArray();
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

    private List<TrainerQuoted>? LoadInfo()
    {
        using Stream stream = AssetLoader.Open(new Uri("avares://FrontierQuotes/Assets/trainers.json"));
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

        int trainerId = RandomNumberGenerator.GetInt32(0, Names.Length);
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
        await NextQuote(true);
    }
    
}
