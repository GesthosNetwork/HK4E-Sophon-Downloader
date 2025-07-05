using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    class Game
    {
        public string GameId { get; set; } = "";

        public enum GameType
        {
            hk4e,
        }

        static readonly Dictionary<(Region, GameType), string> gameMap = new()
        {
            {(Region.OSREL, GameType.hk4e), "gopR6Cufr3"},
            {(Region.CNREL, GameType.hk4e), "1Z8W5NHUQb"},
        };

        public Game(Region region, string id)
        {
            bool isRel = !Enum.TryParse(id, out GameType game);
            if (isRel)
            {
                this.GameId = id;
            } else
            {
                this.GameId = gameMap[(region, game)];
            }
        }

        public string GetGameId()
        {
            return this.GameId;
        }
    }
}