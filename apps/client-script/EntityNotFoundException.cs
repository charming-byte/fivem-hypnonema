using System;

namespace Hypnonema.Client;

public sealed class EntityNotFoundException(string message) : Exception(message);