using bot_fy.Components;
using bot_fy.Entitys;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using SpotifyAPI.Web;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace bot_fy.Extensions.Discord
{
    public static class NotificationsDiscordChannelExtensions
    {
        private static readonly DiscordMessages messages = new();

        public static async Task<DiscordMessage> SendNewMusicAsync(this DiscordChannel channel, IVideo video)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = $"Adicionado a fila {video.Title}",
                Color = DiscordColor.Green,
                Url = video.Url
            };
            embed.AddField("Canal", video.Author.ChannelTitle);
            embed.AddField("Tempo", $"{video.Duration.ToStringTime()}", true);
            embed.WithThumbnail(video.Thumbnails.MaxBy(p => p.Resolution.Height)!.Url);

            return await channel.SendMessageAsync(embed);
        }

        public static async Task<DiscordMessage> SendNewPlaylistAsync(this DiscordChannel channel, Playlist playlist, IReadOnlyList<PlaylistVideo> videos)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = $"Adicionado a fila {playlist.Title}",
                Color = DiscordColor.Green,
                Url = playlist.Url
            };
            embed.AddField("Quantidade de músicas", videos.Count.ToString());
            embed.AddField("Duração", $"{videos.Sum(p => p.Duration).ToStringTime()}", true);
            embed.WithThumbnail(playlist.Thumbnails.MaxBy(p => p.Resolution.Height)!.Url);
            return await channel.SendMessageAsync(embed);
        }

        public static async Task<DiscordMessage> SendNewPlaylistSpotify(this DiscordChannel channel, FullPlaylist playlist)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = $"Adicionado a fila {playlist.Name}",
                Color = DiscordColor.Green,
                Url = playlist.ExternalUrls["spotify"]
            };
            embed.AddField("Quantidade de músicas", playlist.Tracks.Total.ToString());
            embed.WithThumbnail(playlist.Images.MaxBy(p => p.Height)!.Url);
            return await channel.SendMessageAsync(embed);
        }

        public static async Task<DiscordMessage> SendNewMusicPlayAsync(this DiscordChannel channel, Music music)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = $"Tocando {music.Name}",
                Color = DiscordColor.Green,
            };
            embed.AddField("Tempo", $"{music.Duration.ToStringTime()}", true);

            DiscordMessageBuilder message = messages.GetMessageBuilderWithControls(embed);

            return await channel.SendMessageAsync(message);
        }

        public static async Task SendPaginatedMusicsAsync(this DiscordChannel channel, DiscordUser user, IEnumerable<Music> musics)
        {
            int pages_count = musics.Count() / 10;

            List<Page> pages = new();

            for (int i = 0; i < pages_count; i++)
            {
                List<Music> page = musics.Skip(i * 10).Take(10).ToList();

                DiscordEmbedBuilder embed = new()
                {
                    Title = "Fila de Musicas",
                    Color = DiscordColor.Green,
                };

                for (int j = 0; j < page.Count; j++)
                {
                    embed.Description += $"{(i * 10) + j} - {page[j].Name}\n";
                }
                embed.WithFooter($"Página {i + 1} de {pages_count} - {musics.Sum(p => p.Duration).ToStringTime()}");
                pages.Add(new Page("", embed));
            }

            await channel.SendPaginatedMessageAsync(user, pages);
        }

        public static async Task SendPaginatedAvailablesMusicsAsync(this DiscordChannel channel, DiscordUser user, IEnumerable<Music> musics)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = "Musicas Disponiveis",
                Color = DiscordColor.Green,
            };
            foreach (Music music in musics)
            {
                embed.Description += $"{music.Name}\n";
            }

            await channel.SendMessageAsync(embed);
        }
    }
}