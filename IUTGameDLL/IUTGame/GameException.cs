using System;

#nullable disable
namespace IUTGame;

public class GameException(string msg = "") : Exception(msg)
{
}
