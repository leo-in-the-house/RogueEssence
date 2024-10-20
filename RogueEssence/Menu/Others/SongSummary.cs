﻿using RogueElements;
using System.Collections.Generic;
using RogueEssence.Content;
using System;
using System.IO;

namespace RogueEssence.Menu
{
    public class SongSummary : SummaryMenu
    {
        MenuText Name;
        MenuText OriginName;
        MenuText Origin;
        DialogueText Artist;

        public SongSummary(Rect bounds)
            : base(bounds)
        {
            Name = new MenuText("", new Loc(GraphicsManager.MenuBG.TileWidth + 2, GraphicsManager.MenuBG.TileHeight + 2));
            Elements.Add(Name);
            OriginName = new MenuText("", new Loc(GraphicsManager.MenuBG.TileWidth + 2, GraphicsManager.MenuBG.TileHeight + VERT_SPACE + 2));
            Elements.Add(OriginName);
            Origin = new MenuText("", new Loc(GraphicsManager.MenuBG.TileWidth + 2, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * 2 + 2));
            Elements.Add(Origin);
            Artist = new DialogueText("", new Rect(new Loc(GraphicsManager.MenuBG.TileWidth + 2, GraphicsManager.MenuBG.TileHeight + VERT_SPACE * 3 + 2),
                new Loc(Bounds.Width - GraphicsManager.MenuBG.TileWidth * 4, Bounds.Height - GraphicsManager.MenuBG.TileHeight * 4)), LINE_HEIGHT);
            Elements.Add(Artist);
        }

        protected override IEnumerable<IMenuElement> GetDrawElements()
        {
            yield return Name;
            yield return OriginName;

            yield return Origin;
            yield return Artist;

        }

        public void SetSong(string fileName)
        {
            string name = "---";
            string originName = "---";
            string origin = "---";
            string artist = "---";

            if (File.Exists(fileName))
            {
                try
                {
                    LoopedSong song = new LoopedSong(fileName);
                    name = song.Name;
                    if (song.Tags.ContainsKey("TITLE"))
                        originName = song.Tags["TITLE"];
                    if (song.Tags.ContainsKey("ALBUM"))
                        origin = song.Tags["ALBUM"];
                    if (song.Tags.ContainsKey("ARTIST"))
                        artist = song.Tags["ARTIST"];
                }
                catch (Exception ex)
                {
                    DiagManager.Instance.LogError(new Exception("Error loading song " + fileName + "\n", ex));
                }
            }

            Name.SetText(name);
            OriginName.SetText(Text.FormatKey("MENU_SONG_ORIGIN_NAME", originName));
            Origin.SetText(Text.FormatKey("MENU_SONG_ORIGIN", origin));
            Artist.SetAndFormatText(Text.FormatKey("MENU_SONG_ARTIST", artist));
        }
    }
}
