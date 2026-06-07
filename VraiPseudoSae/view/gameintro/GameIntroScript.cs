using System.Collections.Generic;
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

internal enum SettingsIntroCategory
{
    General,
    Controls
}

internal enum SettingsIntroHighlightTarget
{
    Interface,
    CategoryPanel,
    GeneralCategory,
    GeneralSettings,
    MasterVolume,
    DialogueVolume,
    SfxVolume,
    TextSpeed,
    MainMenuCategory,
    KeyBindings,
    KeyBox,
    ChangeButton,
    ResetButton,
    None
}

internal sealed record SettingsIntroStep(
    SettingsIntroCategory Category,
    SettingsIntroHighlightTarget HighlightTarget,
    IReadOnlyList<DialogueSegment> Segments);

internal sealed record SettingsIntroUiText(
    string CategoriesTitle,
    string GeneralCategory,
    string MainMenuCategory,
    string GeneralTitle,
    string MasterVolume,
    string DialogueVolume,
    string SfxVolume,
    string TextSpeed,
    string MainMenuTitle,
    string ActionHeader,
    string KeyHeader,
    string ForwardAction,
    string BackwardAction,
    string LeftAction,
    string RightAction,
    string InteractionAction,
    string ChangeKey,
    string PressKey,
    string CloseTemporary);

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
                new DialogueSegment("game settings", DialogueTextStyle.ControlsWord),
                new DialogueSegment(". Interact with the sign using the key shown above it !", DialogueTextStyle.Normal)
            };
        }

        return new[]
        {
            new DialogueSegment("Impressionnant non ? Tu dois te demander à quoi il sert ? Il sert à changer tes ", DialogueTextStyle.Normal),
            new DialogueSegment("paramètres de jeu", DialogueTextStyle.ControlsWord),
            new DialogueSegment(". Interagis avec le panneau avec la touche affichée au-dessus !", DialogueTextStyle.Normal)
        };
    }

    public static SettingsIntroUiText SettingsUi(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new SettingsIntroUiText(
                "Categories",
                "General",
                "Controls",
                "General",
                "Master volume",
                "Dialogue volume",
                "SFX volume",
                "Text speed",
                "Controls",
                "Action",
                "Assigned key",
                "Move up",
                "Move down",
                "Move left",
                "Move right",
                "Interact",
                "Change key",
                "Press a key",
                "Esc: temporary close");
        }

        return new SettingsIntroUiText(
            "Catégories",
            "Général",
            "Contrôles",
            "Général",
            "Volume général",
            "Volume des dialogues",
            "Volume des SFX",
            "Vitesse du texte",
            "Contrôles",
            "Action",
            "Touche attribuée",
            "Avancer",
            "Reculer",
            "Aller à gauche",
            "Aller à droite",
            "Interagir",
            "Changer la touche",
            "Appuyez sur une touche",
            "Échap : fermer temporairement");
    }

    public static IReadOnlyList<SettingsIntroStep> SettingsTutorial(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.Interface,
                    Normal("Here is the settings interface. This intro version only keeps useful options: audio, dialogue speed, movement, and interaction.")),
                Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.CategoryPanel,
                    Normal("The left panel chooses the settings family. For now, there are only two categories because the rest would just get in your way.")),
                Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.GeneralCategory,
                    Normal("We start with "),
                    Controls("General"),
                    Normal(", where every change is saved immediately. Move a slider, and the game keeps the new value.")),
                Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.GeneralSettings,
                    Normal("The right panel shows the selected category. These settings affect the game right away, not after a confirmation screen.")),
                Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.MasterVolume,
                    Normal("Master volume controls the whole mix. Lower it and every sound follows.")),
                Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.DialogueVolume,
                    Normal("The dialogue volume only changes voices and text blips, so you can keep conversations readable without touching the rest.")),
                Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.SfxVolume,
                    Normal("The SFX volume is for clicks, impacts, portals, and all the small sounds that make the hub feel alive.")),
                Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.TextSpeed,
                    Normal("Text speed changes how quickly dialogue appears. You can slow me down or make me talk faster if you prefer.")),
                Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.MainMenuCategory,
                    Normal("Now look at "),
                    Controls("Controls"),
                    Normal(". It contains only the keys you use here: moving around and interacting.")),
                Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.KeyBindings,
                    Normal("Each line has one action, its assigned key, a change button, and a reset button for the default key.")),
                Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.KeyBox,
                    Normal("The key box shows the active key. If you bind interaction to P, the sign will show P and will react to P.")),
                Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.ChangeButton,
                    Normal("The "),
                    Controls("Change key"),
                    Normal(" button waits for your next key press, then saves the new binding automatically.")),
                Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.ResetButton,
                    Normal("The curved arrow restores the default key for that action. Useful when a test binding does not feel right.")),
                Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.None,
                    Normal("You can temporarily close this panel with Esc or the X button, test your controls in the hub, then interact with the sign again to come back."))
            };
        }

        return new[]
        {
            Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.Interface,
                Normal("Voilà l'interface des paramètres. Pour cette introduction, elle ne garde que les options utiles : audio, vitesse du texte, déplacement et interaction.")),
            Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.CategoryPanel,
                Normal("Le panneau de gauche choisit la famille de paramètres. Pour l'instant, il n'y a que deux catégories parce que le reste te ralentirait plus qu'autre chose.")),
            Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.GeneralCategory,
                Normal("On commence avec "),
                Controls("Général"),
                Normal(", où chaque changement est sauvegardé immédiatement. Tu bouges un curseur, le jeu garde la nouvelle valeur.")),
            Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.GeneralSettings,
                Normal("À droite, tu vois les paramètres de la catégorie choisie. Ces réglages s'appliquent tout de suite, sans écran de confirmation.")),
            Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.MasterVolume,
                Normal("Le volume général contrôle tout le mélange sonore. Si tu le baisses, tous les sons suivent.")),
            Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.DialogueVolume,
                Normal("Le volume des dialogues règle seulement les voix et les petits bips du texte, pratique si tu veux mieux suivre les conversations.")),
            Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.SfxVolume,
                Normal("Le volume des SFX concerne les clics, les impacts, les portails, et tous les petits sons qui donnent de la vie au hub.")),
            Step(SettingsIntroCategory.General, SettingsIntroHighlightTarget.TextSpeed,
                Normal("La vitesse du texte change le rythme d'apparition des dialogues. Tu peux me ralentir ou me faire parler plus vite selon ce que tu préfères.")),
            Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.MainMenuCategory,
                Normal("Maintenant, regarde "),
                Controls("Contrôles"),
                Normal(". Elle contient seulement les touches utiles ici : se déplacer et interagir.")),
            Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.KeyBindings,
                Normal("Chaque ligne représente une action, sa touche attribuée, un bouton pour la changer, et un bouton pour remettre la touche par défaut.")),
            Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.KeyBox,
                Normal("La case de touche affiche le raccourci actif. Si tu mets l'interaction sur P, le panneau affichera P et réagira à P.")),
            Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.ChangeButton,
                Normal("Le bouton "),
                Controls("Changer la touche"),
                Normal(" met la ligne en attente. Ensuite, tu appuies sur une touche, et le nouveau raccourci est sauvegardé automatiquement.")),
            Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.ResetButton,
                Normal("La flèche arrondie remet la touche par défaut de cette action. Pratique quand un test ne donne pas ce que tu voulais.")),
            Step(SettingsIntroCategory.Controls, SettingsIntroHighlightTarget.None,
                Normal("Tu peux fermer temporairement ce panneau avec Échap ou le bouton X, tester tes touches dans le hub, puis réinteragir avec le panneau pour revenir."))
        };
    }

    private static IReadOnlyList<DialogueSegment> Block(string text)
    {
        return new[] { new DialogueSegment(text, DialogueTextStyle.Normal) };
    }

    private static DialogueSegment Normal(string text)
    {
        return new DialogueSegment(text, DialogueTextStyle.Normal);
    }

    private static DialogueSegment Controls(string text)
    {
        return new DialogueSegment(text, DialogueTextStyle.ControlsWord);
    }

    private static SettingsIntroStep Step(
        SettingsIntroCategory category,
        SettingsIntroHighlightTarget highlightTarget,
        params DialogueSegment[] segments)
    {
        return new SettingsIntroStep(category, highlightTarget, segments);
    }
}
