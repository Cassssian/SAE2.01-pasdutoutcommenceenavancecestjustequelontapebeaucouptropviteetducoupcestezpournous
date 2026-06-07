using System.Collections.Generic;
using VraiPseudoSae.Utils.AudioPlayer;

namespace VraiPseudoSae.view.gameintro;

internal enum GameIntroLanguage
{
    French,
    English
}

internal enum DialogueTextStyle
{
    Normal,
    PanelWord,
    ControlsWord,
    Rainbow
}

internal sealed record DialogueSegment(string Text, DialogueTextStyle Style);

internal static class GameIntroScript
{
    public static string SpaceKey(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.French ? "Espace" : "Space";
    }

    public static string LanguageChoiceTitle(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.French
            ? "Choisis ta langue"
            : "Choose your language";
    }

    public static string LanguageChoiceHelp(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.French
            ? "Entree ou Espace : valider"
            : "Enter or Space: confirm";
    }

    public static string FrenchLabel(GameIntroLanguage language)
    {
        return "Français";
    }

    public static string EnglishLabel(GameIntroLanguage language)
    {
        return "English";
    }

    public static IReadOnlyList<IReadOnlyList<DialogueSegment>> OpeningBlocks(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                Block("Hey you ? Are you okay ? You're new here, right ? Welcome to RetroHub, the crossroads where every mini-game stays alive as long as its stars keep shining."),
                Block("Tonight, the central arcade cabinet shook, the portals went wild, and my stars broke loose before hiding inside each game."),
                Block("Without them, I can't launch my favorite games properly. Help me find them, and I will be able to bring the whole hub back to life.")
            };
        }

        return new[]
        {
            Block("Hey toi ? Ça va ? Tu es nouveau ici nan ? Bienvenue dans RetroHub, le carrefour où tout les mini-jeux restent allumés tant que leurs étoiles brillent !"),
            Block("Cette nuit, la borne centrale a tremblé, les portails se sont emballés, et mes étoiles se sont détachées avant de filer se cacher dans chaque jeu."),
            Block("Sans elles, impossible de lancer mes parties favorites correctement. Aide-moi à les retrouver, et je pourrai remettre tout le hub en marche.")
        };
    }

    public static IReadOnlyList<DialogueSegment> PrankExplanation(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                new DialogueSegment("Oops !! I forgot to explain how things work here ! See that ", DialogueTextStyle.Normal),
                new DialogueSegment("sign", DialogueTextStyle.PanelWord),
                new DialogueSegment("next to me?", DialogueTextStyle.Normal)
            };
        }

        return new[]
        {
            new DialogueSegment("Oups !! J'ai oublié de t'expliquer comment ça fonctionne ici ! Tu vois ce ", DialogueTextStyle.Normal),
            new DialogueSegment("panneau", DialogueTextStyle.PanelWord),
            new DialogueSegment("à côté de moi ?", DialogueTextStyle.Normal)
        };
    }

    public static IReadOnlyList<DialogueSegment> MagicPanelReveal(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                new DialogueSegment("Well, this is the ", DialogueTextStyle.Normal),
                new DialogueSegment("sign", DialogueTextStyle.PanelWord),
                new DialogueSegment("made of ancient magic wood, keeper of a thousand mysterious secrets, carved with strange marks and loaded with ridiculously powerful hidden powers from a forgotten age, protected by a giga mega hyper supreme mystical energy of eternal infinity !", DialogueTextStyle.Rainbow)
            };
        }

        return new[]
        {
            new DialogueSegment("Eh bah c'est le ", DialogueTextStyle.Normal),
            new DialogueSegment("panneau", DialogueTextStyle.PanelWord),
            new DialogueSegment(" en bois magique ultra ancien des mille secrets mystérieux avec des marques étranges gravées dessus et des pouvoirs cachés incroyablement puissants venu d'un temps oublié et protégé par une énergie mystique giga mega hyper suprême de l'infini éternel !", DialogueTextStyle.Rainbow)
        };
    }

    public static IReadOnlyList<DialogueSegment> InteractionHint(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                new DialogueSegment("Impressive, right ? You're probably wondering what it does. It lets you change your ", DialogueTextStyle.Normal),
                new DialogueSegment("keyboard controls", DialogueTextStyle.ControlsWord),
                new DialogueSegment(". Interact with the sign using the E key !", DialogueTextStyle.Normal)
            };
        }

        return new[]
        {
            new DialogueSegment("Impressionnant non ? Tu dois te demander à quoi il sert ? Il sert à changer tes ", DialogueTextStyle.Normal),
            new DialogueSegment("touches de clavier", DialogueTextStyle.ControlsWord),
            new DialogueSegment(". Interagis avec le panneau avec la touche E !", DialogueTextStyle.Normal)
        };
    }

    private static IReadOnlyList<DialogueSegment> Block(string text)
    {
        return new[] { new DialogueSegment(text, DialogueTextStyle.Normal) };
    }
}
