using System;
using Zero.Hotel.GameClients;
using Zero.Hotel.Pathfinding;
using Zero.Hotel.Rooms;

namespace Zero.Hotel.RoomBots;

internal class PetBot : BotAI
{
    private int SpeechTimer;

    private int ActionTimer;

    private int EnergyTimer;

    public PetBot(int VirtualId)
    {
        SpeechTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 60);
        ActionTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 30 + VirtualId);
        EnergyTimer = new Random((VirtualId ^ 2) + DateTime.Now.Millisecond).Next(10, 60);
    }

    private void RemovePetStatus()
    {
        RoomUser Pet = GetRoomUser();
        Pet.Statusses.Remove("sit");
        Pet.Statusses.Remove("lay");
        Pet.Statusses.Remove("snf");
        Pet.Statusses.Remove("eat");
        Pet.Statusses.Remove("ded");
        Pet.Statusses.Remove("jmp");
    }

    public override void OnSelfEnterRoom()
    {
        int randomX = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeX);
        int randomY = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeY);
        GetRoomUser().MoveTo(randomX, randomY);
    }

    public override void OnSelfLeaveRoom(bool Kicked)
    {
    }

    public override void OnUserEnterRoom(RoomUser User)
    {
        if (User.GetClient().GetHabbo().Username.ToLower() == GetRoomUser().PetData.OwnerName.ToLower())
        {
            GetRoomUser().Chat(null, "*Pensando no meu Dono*", Shout: false);
        }
    }

    public override void OnUserLeaveRoom(GameClient Client)
    {
    }

    public override void OnUserSay(RoomUser User, string Message)
    {
        RoomUser Pet = GetRoomUser();
        if (Message.ToLower().Equals(Pet.PetData.Name.ToLower()))
        {
            Pet.SetRot(Rotation.Calculate(Pet.X, Pet.Y, User.X, User.Y));
            return;
        }
        if (Message.ToLower().StartsWith(Pet.PetData.Name.ToLower() + " ") && User.GetClient().GetHabbo().Username.ToLower() == GetRoomUser().PetData.OwnerName.ToLower())
        {
            string Command = Message.Substring(Pet.PetData.Name.ToLower().Length + 1);
            int r = HolographEnvironment.GetRandomNumber(1, 8);
            if ((Pet.PetData.Energy > 10 && r < 6) || Pet.PetData.Level > 15)
            {
                RemovePetStatus();
                switch (Command)
                {
                    case "free":
                    case "Free":
                    case "livre":
                        {
                            RemovePetStatus();
                            int randomX = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeX);
                            int randomY = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeY);
                            Pet.MoveTo(randomX, randomY);
                            Pet.PetData.AddExpirience(10);
                            break;
                        }
                    case "come":
                    case "here":
                    case "Here":
                    case "aqui":
                        {
                            RemovePetStatus();
                            int NewX = User.X;
                            int NewY = User.Y;
                            ActionTimer = 30;
                            if (User.RotBody == 4)
                            {
                                NewY = User.Y + 1;
                            }
                            else if (User.RotBody == 0)
                            {
                                NewY = User.Y - 1;
                            }
                            else if (User.RotBody == 6)
                            {
                                NewX = User.X - 1;
                            }
                            else if (User.RotBody == 2)
                            {
                                NewX = User.X + 1;
                            }
                            else if (User.RotBody == 3)
                            {
                                NewX = User.X + 1;
                                NewY = User.Y + 1;
                            }
                            else if (User.RotBody == 1)
                            {
                                NewX = User.X + 1;
                                NewY = User.Y - 1;
                            }
                            else if (User.RotBody == 7)
                            {
                                NewX = User.X - 1;
                                NewY = User.Y - 1;
                            }
                            else if (User.RotBody == 5)
                            {
                                NewX = User.X - 1;
                                NewY = User.Y + 1;
                            }
                            Pet.PetData.AddExpirience(10);
                            Pet.MoveTo(NewX, NewY);
                            break;
                        }
                    case "senta":
                    case "sit":
                    case "Sit":
                        RemovePetStatus();
                        Pet.PetData.AddExpirience(10);
                        Pet.Statusses.Add("sit", Pet.Z.ToString());
                        ActionTimer = 25;
                        EnergyTimer = 10;
                        break;
                    case "FDP":
                    case "fdp":
                    case "Se Fode":
                    case "Retardado":
                    case "retardado":
                        {
                            RemovePetStatus();
                            Pet.PetData.AddExpirience(10);
                            string[] Fala = new string[5] { "*Morre seu merda*", "Não pedi pra ser meu dono", "Quem Programou Você?", "*Otário*", "Huuuuuuum, Boiola" };
                            Random FalaRandomica = new Random();
                            Pet.Chat(null, Fala[FalaRandomica.Next(0, Fala.Length - 1)], Shout: false);
                            break;
                        }
                    case "lay":
                    case "down":
                    case "Down":
                    case "abaixa":
                    case "Lay":
                    case "deita":
                        RemovePetStatus();
                        Pet.Statusses.Add("lay", Pet.Z.ToString());
                        Pet.PetData.AddExpirience(10);
                        ActionTimer = 30;
                        EnergyTimer = 5;
                        break;
                    case "play dead":
                    case "dead":
                    case "Dead":
                    case "morre":
                        RemovePetStatus();
                        Pet.Statusses.Add("ded", Pet.Z.ToString());
                        Pet.PetData.AddExpirience(10);
                        SpeechTimer = 45;
                        ActionTimer = 30;
                        break;
                    case "sleep":
                    case "Sleep":
                    case "dorme":
                        RemovePetStatus();
                        Pet.Chat(null, "ZzzZZZzzzzZzz", Shout: false);
                        Pet.Statusses.Add("lay", Pet.Z.ToString());
                        Pet.PetData.AddExpirience(10);
                        EnergyTimer = 5;
                        SpeechTimer = 30;
                        ActionTimer = 45;
                        break;
                    case "jump":
                    case "Jump":
                    case "pula":
                        RemovePetStatus();
                        Pet.Statusses.Add("jmp", Pet.Z.ToString());
                        Pet.PetData.AddExpirience(10);
                        EnergyTimer = 5;
                        SpeechTimer = 10;
                        ActionTimer = 5;
                        break;
                    default:
                        {
                            string[] Speech = new string[5] { "*Confundido*", "Não Intendo", "Oque quer?", "Quê isso?", "Huuuuuuuuum" };
                            Random RandomSpeech = new Random();
                            Pet.Chat(null, Speech[RandomSpeech.Next(0, Speech.Length - 1)], Shout: false);
                            break;
                        }
                }
                Pet.PetData.PetEnergy(Add: false);
                Pet.PetData.PetEnergy(Add: false);
            }
            else
            {
                RemovePetStatus();
                if (Pet.PetData.Energy < 10)
                {
                    string[] Speech = new string[7] { "ZzZzzzzz", "*Tô cansado*", "Cansado *Está cansado*", "ZzZzZZzzzZZz", "zzZzzZzzz", "... Com Sonoo ..", "ZzZzzZ" };
                    Random RandomSpeech = new Random();
                    Pet.Chat(null, Speech[RandomSpeech.Next(0, Speech.Length - 1)], Shout: false);
                    Pet.Statusses.Add("lay", Pet.Z.ToString());
                    SpeechTimer = 50;
                    ActionTimer = 45;
                    EnergyTimer = 5;
                }
                else
                {
                    string[] Speech = new string[8] { "*Pensando*", "*Não :]*", " ... ", "Quem você pensa que é?", "Whaaaaaaaaat?", "Grrrrr", "*Teenso*", "Por quê?" };
                    Random RandomSpeech = new Random();
                    Pet.Chat(null, Speech[RandomSpeech.Next(0, Speech.Length - 1)], Shout: false);
                    Pet.PetData.PetEnergy(Add: false);
                }
            }
        }
        Pet = null;
    }

    public override void OnUserShout(RoomUser User, string Message)
    {
    }

    public override void OnTimerTick()
    {
        if (SpeechTimer <= 0)
        {
            RoomUser Pet = GetRoomUser();
            if (Pet != null)
            {
                Random RandomSpeech = new Random();
                RemovePetStatus();
                if (Pet.PetData.Type == 0 || Pet.PetData.Type == 3)
                {
                    string[] Speech = new string[8] { "woof woof woof", "Auuuu auuuu", "wooooof", "Grrrr", "Sentandose", "*Alouca  HAHA*", "", "Woof" };
                    string rSpeech = Speech[RandomSpeech.Next(0, Speech.Length - 1)];
                    if (rSpeech.Length != 3)
                    {
                        Pet.Chat(null, rSpeech, Shout: false);
                    }
                    else
                    {
                        Pet.Statusses.Add(rSpeech, Pet.Z.ToString());
                    }
                }
                else if (Pet.PetData.Type == 1)
                {
                    string[] Speech = new string[6] { "miauu", "Hmmmm", "*Estornudando", "*Lambe Pata*", "Sentandose", "Oliendo" };
                    string rSpeech = Speech[RandomSpeech.Next(0, Speech.Length - 1)];
                    if (rSpeech.Length != 3)
                    {
                        Pet.Chat(null, rSpeech, Shout: false);
                    }
                    else
                    {
                        Pet.Statusses.Add(rSpeech, Pet.Z.ToString());
                    }
                }
                else if (Pet.PetData.Type == 2)
                {
                    string[] Speech = new string[7] { "Rrrr....Grrrrrg....", "*Abrir boca*", "Tick tock tick....", "*Bocejando*", "Mover cola", "Tô Cansado", "Estornudando" };
                    string rSpeech = Speech[RandomSpeech.Next(0, Speech.Length - 1)];
                    if (rSpeech.Length != 3)
                    {
                        Pet.Chat(null, rSpeech, Shout: false);
                    }
                    else
                    {
                        Pet.Statusses.Add(rSpeech, Pet.Z.ToString());
                    }
                }
                else if (Pet.PetData.Type == 4)
                {
                    string[] Speech = new string[6] { "*Que Fooomeeee*", "Grrrrrrr", "*Estornudando*", "*Que tédio*.", "Grrrr... grrrr", "Tô cansado" };
                    string rSpeech = Speech[RandomSpeech.Next(0, Speech.Length - 1)];
                    if (rSpeech.Length != 3)
                    {
                        Pet.Chat(null, rSpeech, Shout: false);
                    }
                    else
                    {
                        Pet.Statusses.Add(rSpeech, Pet.Z.ToString());
                    }
                }
                else if (Pet.PetData.Type == 5)
                {
                    string[] Speech = new string[8] { "Oink Oink..", "*Meando*", "Estornudando", "*Tirandose un pedo*", "Oink!", "*Hacer el cerdo*", "Estoy cansado", "oink" };
                    string rSpeech = Speech[RandomSpeech.Next(0, Speech.Length - 1)];
                    if (rSpeech.Length != 3)
                    {
                        Pet.Chat(null, rSpeech, Shout: false);
                    }
                    else
                    {
                        Pet.Statusses.Add(rSpeech, Pet.Z.ToString());
                    }
                }
                else if (Pet.PetData.Type == 6)
                {
                    string[] Speech = new string[10] { "Agr...", "Grrrrr.... grrrr....", "Grrrrr...rawh!", "snf", "Grrrrrrh...", "snf", "lay", "Grr...", "*rugiendo*", "*rugido*" };
                    string rSpeech = Speech[RandomSpeech.Next(0, Speech.Length - 1)];
                    if (rSpeech.Length != 3)
                    {
                        Pet.Chat(null, rSpeech, Shout: false);
                    }
                    else
                    {
                        Pet.Statusses.Add(rSpeech, Pet.Z.ToString());
                    }
                }
                else if (Pet.PetData.Type == 7)
                {
                    string[] Speech = new string[8] { "Auguruuuh...", "Buff", "Augubuff...", "Buffuu...", "*sueño*", "snf", "lay", "Aff" };
                    string rSpeech = Speech[RandomSpeech.Next(0, Speech.Length - 1)];
                    if (rSpeech.Length != 3)
                    {
                        Pet.Chat(null, rSpeech, Shout: false);
                    }
                    else
                    {
                        Pet.Statusses.Add(rSpeech, Pet.Z.ToString());
                    }
                }
                Pet = null;
            }
            SpeechTimer = HolographEnvironment.GetRandomNumber(20, 120);
        }
        else
        {
            SpeechTimer--;
        }
        if (ActionTimer <= 0)
        {
            RemovePetStatus();
            int randomX = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeX);
            int randomY = HolographEnvironment.GetRandomNumber(0, GetRoom().Model.MapSizeY);
            GetRoomUser().MoveTo(randomX, randomY);
            ActionTimer = HolographEnvironment.GetRandomNumber(15, 40 + GetRoomUser().PetData.VirtualId);
        }
        else
        {
            ActionTimer--;
        }
        if (EnergyTimer <= 0)
        {
            RemovePetStatus();
            RoomUser Pet2 = GetRoomUser();
            Pet2.PetData.PetEnergy(Add: true);
            EnergyTimer = HolographEnvironment.GetRandomNumber(30, 120);
        }
        else
        {
            EnergyTimer--;
        }
    }
}
