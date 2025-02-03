using bot_fy.Entitys;
using DSharpPlus.Entities;

namespace bot_fy.Service
{
    public class MusicService
    {
        string folderPath = Environment.GetEnvironmentVariable("MUSIC_PATH");

        public async Task<List<Music>> GetMusics()
        {
            var files = Directory.GetFiles(folderPath, "*.mp3", SearchOption.AllDirectories);

            List<Music> musicList = new List<Music>();

            foreach (var file in files)
            {
                try
                {
                    var tagFile = TagLib.File.Create(file);
                    Music music = new()
                    {
                        Name = tagFile.Tag.Title ?? Path.GetFileNameWithoutExtension(file),
                        Author = string.Join(", ", tagFile.Tag.Performers) ?? "Desconhecido",
                        Duration = tagFile.Properties.Duration,
                        Path = file

                    };
                    musicList.Add(music);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao processar o arquivo {file}: {ex.Message}");
                }
            }

            return musicList;
        }

        public async Task<List<Music>> GetMusicOrPlayList(string termo, DiscordChannel channel)
        {
            string possibleFolderPath = Path.Combine(folderPath, termo.ToLower());

            if (Directory.Exists(possibleFolderPath))
            {
                var files = Directory.GetFiles(folderPath, "*.mp3", SearchOption.AllDirectories);

                List<Music> musicList = new List<Music>();

                foreach (var file in files)
                {
                    try
                    {
                        var tagFile = TagLib.File.Create(file);
                        Music music = new()
                        {
                            Name = tagFile.Tag.Title ?? Path.GetFileNameWithoutExtension(file),
                            Author = string.Join(", ", tagFile.Tag.Performers) ?? "Desconhecido",
                            Duration = tagFile.Properties.Duration,
                            Path = file

                        };
                        musicList.Add(music);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao processar o arquivo {file}: {ex.Message}");
                    }
                }

                return musicList;
            }
            else
            {
                var files = Directory.GetFiles(folderPath, "*.mp3", SearchOption.AllDirectories);
                var foundFile = files.FirstOrDefault(file => Path.GetFileNameWithoutExtension(file).Equals(termo, StringComparison.OrdinalIgnoreCase));

                if (foundFile != null)
                {
                    try
                    {
                        var tagFile = TagLib.File.Create(foundFile);
                        return new List<Music>
                        {
                            new Music
                            {
                                Name = tagFile.Tag.Title ?? Path.GetFileNameWithoutExtension(foundFile),
                                Author = string.Join(", ", tagFile.Tag.Performers) ?? "Desconhecido",
                                Duration = tagFile.Properties.Duration,
                                Path = foundFile
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao processar o arquivo {foundFile}: {ex.Message}");
                    }
                }
                await channel.SendMessageAsync($"Nenhuma música ou playlist encontrada para o termo: {termo}");
                return new List<Music>();
            }
        }
    }
}
