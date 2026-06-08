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
    Rainbow,
    StarWord,
    TileWord,
    AccessWord,
    Shake,
    Italic,
    StarCount
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

    public static IReadOnlyList<DialogueSegment> PostSettingsTourDone(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? Block("There ! That's it for the tiny intro tou-")
            : Block("Voilà ! C'est fini pour le petit tour d'intro-");
    }

    public static IReadOnlyList<DialogueSegment> CenterCabinetShake(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? Block("OH NOOOO ! THE CENTRAL CABINET IS SHAKING AGAIN !!")
            : Block("OH NOOOOON ! LA BORNE CENTRE SE REMET À TREMBLER !!");
    }

    public static IReadOnlyList<DialogueSegment> StarAndTileDiscovery(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                Normal("Oh ! Look ! That's a "),
                Star("game star"),
                Normal(" !")
            };
        }

        return new[]
        {
            Normal("Oh ! Regarde ! C'est une "),
            Star("étoile de jeu"),
            Normal(" !")
        };
    }

    public static IReadOnlyList<DialogueSegment> TileDiscovery(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                Normal("And over there, that's a "),
                Tile("tile"),
                Normal(" that "),
                Access("lets us access a game"),
                Normal(" ! Let's get closer to the star and see if we can do something !")
            };
        }

        return new[]
        {
            Normal("Et là, c'est une "),
            Tile("case"),
            Normal(" qui nous "),
            Access("permet d'accéder à un jeu"),
            Normal(" ! Allons-nous approcher près de l'étoile pour voir si on peut faire quelque chose !")
        };
    }

    public static IReadOnlyList<DialogueSegment> StarApproach(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? Block("Ooooooh, it shines so much, it's such a beautiful star !")
            : Block("Ohhhhhhhhh, qu'est ce qu'elle brille, qu'est ce qu'elle est belle cette étoile !");
    }

    public static IReadOnlyList<DialogueSegment> StarInteractionPanic(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? new[] { Shake("OH NO ! WHAT DID YOU DO ?!") }
            : new[] { Shake("OH NON ! QU'EST CE QUE TU AS FAIT ?!") };
    }

    public static IReadOnlyList<DialogueSegment> StarAbsorbedPanic(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? new[] { Shake("AAHHHHH !! I'M GOING TO DIE !!! WHY DID YOU DO THAT !!") }
            : new[] { Shake("AAHHHHH !! JE VAIS MOURIR !!! POURQUOI TU AS FAIS ÇA !!") };
    }

    public static IReadOnlyList<DialogueSegment> Ellipsis()
    {
        return Block("...");
    }

    public static IReadOnlyList<DialogueSegment> WeirdAttraction(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                Normal("Well, I feel fine after all.... But I feel weird, like something is pulling me toward the "),
                Tile("tile"),
                Normal(".")
            };
        }

        return new[]
        {
            Normal("Bon, je vais bien finalement.... Mais je me sens bizarre, comme attiré vers la "),
            Tile("case"),
            Normal(".")
        };
    }

    public static IReadOnlyList<DialogueSegment> StarHudExplanation(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                Normal("And did you see this ? You can see the "),
                StarCount("number of stars you currently own"),
                Normal(" here.")
            };
        }

        return new[]
        {
            Normal("Et tu as vu ici ? Tu peux voir le "),
            StarCount("nombre d'étoiles que tu possèdes actuellement"),
            Normal(".")
        };
    }

    public static IReadOnlyList<DialogueSegment> BrokenTileBeforeAttack(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                Normal("Oh ? The "),
                Tile("game tile"),
                Normal(" looks weird... Looks like it's busted... Maybe "),
                Rainbow("one good hit"),
                Normal(" should make it work again.")
            };
        }

        return new[]
        {
            Normal("Oh tiens ? La "),
            Tile("case de jeu"),
            Normal(" est bizarre.. On dirait qu'elle est H.S... Peut-être "),
            Rainbow("un bon coup dedans"),
            Normal(" devrait la refaire fonctionner.")
        };
    }

    public static IReadOnlyList<DialogueSegment> AttackPainRant(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? new[] { Shake("F**** H***, THAT HURTS !! WHO INSTALLED A TILE THIS HARD ?! MY HAND HUUUUURTS !!") }
            : new[]
            {
                Shake("PU**** DE ME***, ÇA FAIT MAL CETTE ME*** !! AÀ@D##UÌ}&HIPP ! QUI EST LE CO***** DE ME*** QUI A INSTALLÉ CETTE CHI**** DURE COMME MA B*** !! AHHHHHH MA MAIN J'AI MAAAAAALLLL !! JE SUIS SÛR QU'IL SERAIT CAPABLE D'INSTALLER DES BLOQUEURS DE FENÊTRE EN PLEINE CANICULE CET EN**** DE ME*** ! AHHH JE SUIS SÛR QUE JE ME SUIS CASSÉ LA MAIN, FAIS CH***, F** !")
            };
    }

    public static IReadOnlyList<IReadOnlyList<DialogueSegment>> AttackPainRantPages(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                new[] { Shake("F**** H***, THAT HURTS !!") },
                new[] { Shake("WHO INSTALLED A TILE THIS HARD ?! MY HAND HUUUUURTS !!") },
                new[] { Shake("I SWEAR THIS THING WAS MADE TO BREAK INTERNS !!") }
            };
        }

        return new[]
        {
            new[] { Shake("PU**** DE ME***, ÇA FAIT MAL CETTE ME*** !! AÀ@D##UÌ}&HIPP !") },
            new[] { Shake("QUI EST LE CO***** DE ME*** QUI A INSTALLÉ CETTE CHI**** DURE COMME MA B*** !!") },
            new[] { Shake("AHHHHHH MA MAIN J'AI MAAAAAALLLL !! JE SUIS SÛR QU'IL SERAIT CAPABLE D'INSTALLER DES BLOQUEURS DE FENÊTRE EN PLEINE CANICULE CET EN**** DE ME*** !") },
            new[] { Shake("AHHH JE SUIS SÛR QUE JE ME SUIS CASSÉ LA MAIN, FAIS CH***, F** !") }
        };
    }

    public static IReadOnlyList<DialogueSegment> Disclaimer(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? new[] { Italic("(we are not targeting anyone, this is just a silly contrast joke, nobody is designated :') )") }
            : new[] { Italic("(on a rien contre vous, on a mis ça juste pour le côté rigolo avec le décalage, on attaque personne, personne n'est visé, on ne veut pas de problème, c'est juste une blague, aucun personnel n'a été désigné :') )") };
    }

    public static IReadOnlyList<DialogueSegment> WhatNow(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? Block("F**** ! WHAT DO WE DO NOW !")
            : Block("PU**** ! COMMENT FAIRE ALORS !");
    }

    public static IReadOnlyList<DialogueSegment> RememberStar(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? Block("Oh, but we have the star !! But how do we use it... I'm only an intern here.. I don't know how to do anything...")
            : Block("Oh mais on a l'étoile !! Mais comment faire... Je suis juste un stagiaire ici.. Je ne sais rien faire...");
    }

    public static IReadOnlyList<DialogueSegment> StarLeavesPanic(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? new[] { Shake("NOOOO, WHAT DID YOU DO AGAIN ! THE STAR IS GONE ! YOU ABSOLUTE DOOFUS !! GREAT JOB !!") }
            : new[] { Shake("NOOOOON, QU'EST CE QUE TU AS ENCORE FAIT ! L'ÉTOILE EST PARTIE ! TRIPLE BUSE !! BRAVO À TOI !!") };
    }

    public static IReadOnlyList<DialogueSegment> TileColorPanic(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? Block("NO WAY ! YOU BROKE EVERYTHING ! YOU LOST THE STAR AND THE TILE IS COMPLETELY BROKEN ! HOW ARE WE SUPPOSED TO REPAIR THE HUB NOW !")
            : Block("MAIS NON ! TU AS TOUT CASSÉ ! TU AS PERDU L'ÉTOILE ET LA CASE EST TOTALEMENT CASSÉE ! COMMENT ON VA RÉPARER LE HUB MAINTENANT !");
    }

    public static IReadOnlyList<DialogueSegment> FlashBlindPanic(GameIntroLanguage language)
    {
        return language == GameIntroLanguage.English
            ? new[] { Shake("AAHHHH MY EYYYYEEEES !!! I CAN'T SEE ANYTHING !"), Normal(" I'M SURE THE TILE EXPLODED.") }
            : new[] { Shake("AHHHHH MES YEUUUUUUUUXXXXX !!! JE NE VOIS PLUS RIEN !"), Normal(" JE SUIS SÛR QUE LA CASE A EXPLOSÉ.") };
    }

    public static IReadOnlyList<DialogueSegment> RepairedApology(GameIntroLanguage language)
    {
        if (language == GameIntroLanguage.English)
        {
            return new[]
            {
                Normal("Well, I deeply apologize for everything I said... "),
                Rainbow("BUT WE REPAIRED THE TILE !!!")
            };
        }

        return new[]
        {
            Normal("Bon bah je m'excuse profondément de tout ce que j'ai pu dire... "),
            Rainbow("MAIS ON A RÉPARÉ LA CASE !!!")
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

    private static DialogueSegment Star(string text)
    {
        return new DialogueSegment(text, DialogueTextStyle.StarWord);
    }

    private static DialogueSegment Tile(string text)
    {
        return new DialogueSegment(text, DialogueTextStyle.TileWord);
    }

    private static DialogueSegment Access(string text)
    {
        return new DialogueSegment(text, DialogueTextStyle.AccessWord);
    }

    private static DialogueSegment Shake(string text)
    {
        return new DialogueSegment(text, DialogueTextStyle.Shake);
    }

    private static DialogueSegment Italic(string text)
    {
        return new DialogueSegment(text, DialogueTextStyle.Italic);
    }

    private static DialogueSegment Rainbow(string text)
    {
        return new DialogueSegment(text, DialogueTextStyle.Rainbow);
    }

    private static DialogueSegment StarCount(string text)
    {
        return new DialogueSegment(text, DialogueTextStyle.StarCount);
    }

    private static SettingsIntroStep Step(
        SettingsIntroCategory category,
        SettingsIntroHighlightTarget highlightTarget,
        params DialogueSegment[] segments)
    {
        return new SettingsIntroStep(category, highlightTarget, segments);
    }
}
