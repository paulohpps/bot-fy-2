using bot_fy.Entitys;
using bot_fy.Extensions.Discord;
using bot_fy.Service;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using System.Diagnostics;

namespace bot_fy.Commands
{
    public class MusicCommand : ApplicationCommandModule
    {
        private static readonly Dictionary<ulong, LinkedList<Music>> track = new();
        private static readonly Dictionary<ulong, bool> loopSingleMusic = new();
        private readonly MusicService musicService = new();
        private readonly AudioService audioService = new();

        private static event EventHandler<ulong> OnMusicSkipped;
        private static event EventHandler<ulong> OnMusicStopped;

        [SlashCommand("play", "Reproduza sua musica ou playlist")]
        public async Task Play(InteractionContext ctx, [Option("Nome", "Nome da musica ou playlist")] string termo)
        {
            if (!await ctx.ValidateChannels()) return;

            await ctx.CreateResponseAsync("Buscando...");

            List<Music> musics = await musicService.GetMusicOrPlayList(termo, ctx.Channel);

            if (!musics.Any())
            {
                await ctx.Channel.SendMessageAsync("Nenhuma Musica Encontrada");
                return;
            }

            track.TryAdd(ctx.Guild.Id, new LinkedList<Music>());

            musics.ForEach(v => track[ctx.Guild.Id].AddLast(v));

            VoiceNextExtension vnext = ctx.Client.GetVoiceNext();
            VoiceNextConnection connection = vnext.GetConnection(ctx.Guild);

            if (connection == null)
            {
                DiscordChannel? channel = ctx.Member.VoiceState?.Channel;
                connection = await channel.ConnectAsync();
            }
            else
            {
                return;
            }
            VoiceTransmitSink transmit = connection.GetTransmitSink();
            CancellationTokenSource cancellationToken = new();
            CancellationToken token = cancellationToken.Token;

            connection.UserLeft += async (voice, args) =>
            {
                if (args.User.IsCurrent)
                {
                    track[ctx.Guild.Id].Clear();
                    cancellationToken.Cancel();
                    await ctx.Channel.SendMessageAsync("Saindo do canal de voz");
                    return;
                }

                if (voice.TargetChannel.Users.Count == 1 && voice.TargetChannel.Users.Any(p => p.IsCurrent))
                {
                    track[ctx.Guild.Id].Clear();
                    cancellationToken.Cancel();
                    connection.Dispose();
                    await ctx.Channel.SendMessageAsync("Saindo do canal de voz");
                    return;
                }
            };

            OnMusicSkipped += (obj, guild_id) =>
            {
                if (guild_id == ctx.Guild.Id)
                {
                    cancellationToken.Cancel();
                }
            };

            OnMusicStopped += (obj, guild_id) =>
            {
                if (guild_id == ctx.Guild.Id)
                {
                    track[guild_id].Clear();
                    cancellationToken.Cancel();
                    connection.Dispose();
                    return;
                }
            };

            for (int i = 0; i < track[ctx.Guild.Id].Count; i = 0)
            {
                cancellationToken = new();
                token = cancellationToken.Token;
                await Task.Run(async () =>
                {
                    Music music = track[ctx.Guild.Id].First();
                    track[ctx.Guild.Id].RemoveFirst();
                    DiscordMessage message = await ctx.Channel.SendNewMusicPlayAsync(music);
                    Stream pcm = null;
                    try
                    {
                        Process process = await audioService.ConvertAudioToPcm(music.Path, token);
                        token.Register(process.Kill);
                        process.ErrorDataReceived += (s, e) =>
                        {
                            cancellationToken.Cancel();
                            Console.WriteLine($"Ocorreu um erro e foi cancelado a reprodução {e.Data}");
                        };
                        pcm = process.StandardOutput.BaseStream;
                        await pcm.CopyToAsync(transmit, null, token);
                        await pcm.DisposeAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        await pcm.DisposeAsync();
                    }
                    if (loopSingleMusic[ctx.Guild.Id] && track[ctx.Guild.Id].Count == 0)
                    {
                        track[ctx.Guild.Id].AddFirst(music);
                    }
                    await message.DeleteAsync();
                });
            }
            connection.Disconnect();
            return;
        }

        [SlashCommand("shuffle", "Deixa a fila de musicas aleatoria")]
        public async Task Shuffle(InteractionContext ctx)
        {
            if (!track.ContainsKey(ctx.Guild.Id))
            {
                await ctx.CreateResponseAsync("Nenhuma musica na fila");
                return;
            }
            Shuffle(ctx.Guild.Id);
            await ctx.CreateResponseAsync("Fila embaralhada");
        }

        [SlashCommand("queue", "Mostra a fila de musicas")]
        public async Task Queue(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("Buscando...");

            if (!track.ContainsKey(ctx.Guild.Id))
            {
                await ctx.CreateResponseAsync("Nenhuma musica na fila");
                return;
            }

            await ctx.Channel.SendPaginatedMusicsAsync(ctx.User, track[ctx.Guild.Id]);
        }

        [SlashCommand("list", "Lista de musicas Disponiveis")]
        public async Task List(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("Buscando...");
            List<Music> musics = await musicService.GetMusics();
            if (!musics.Any())
            {
                await ctx.CreateResponseAsync("Nenhuma musica disponivel");
                return;
            }

            await ctx.Channel.SendPaginatedAvailablesMusicsAsync(ctx.User, musics);
        }

        [SlashCommand("clear", "Limpa a fila de musicas")]
        public async Task Clear(InteractionContext ctx)
        {
            if (!track.ContainsKey(ctx.Guild.Id))
            {
                await ctx.CreateResponseAsync("Nenhuma musica na fila");
                return;
            }
            track[ctx.Guild.Id].Clear();
            await ctx.CreateResponseAsync("Fila limpa");
        }

        [SlashCommand("skip", "Pule a musica atual")]
        public async Task Skip(InteractionContext ctx)
        {
            if (!track.ContainsKey(ctx.Guild.Id))
            {
                await ctx.CreateResponseAsync("Nenhuma musica na fila");
                return;
            }
            Skip(ctx.Guild.Id);
            await ctx.CreateResponseAsync("Musica pulada (eu espero) ");
        }

        public static void Skip(ulong guildId)
        {
            OnMusicSkipped.Invoke(null, guildId);
        }
        public static void Stop(ulong guildId)
        {
            OnMusicStopped.Invoke(null, guildId);
        }
        public static void Shuffle(ulong guildId)
        {
            track[guildId] = ShuffleLinkedList(track[guildId]);
        }
        public static void Loop(ulong guildId)
        {
            loopSingleMusic[guildId] = !loopSingleMusic[guildId];
        }
        public static IEnumerable<Music> GetQueue(ulong guildId)
        {
            return track[guildId];
        }
        private static LinkedList<T> ShuffleLinkedList<T>(LinkedList<T> list)
        {
            Random Rand = new Random();

            int size = list.Count;

            //Shuffle the list
            return new LinkedList<T>(list.OrderBy((o) =>
            {
                return (Rand.Next() % size);
            }));
        }
    }
}