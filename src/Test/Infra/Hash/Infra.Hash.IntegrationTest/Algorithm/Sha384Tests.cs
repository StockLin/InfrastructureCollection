﻿using System.Text;
using Infra.Core.Hash.Abstractions;
using Infra.Core.Hash.Enums;
using Infra.Core.Hash.Models;
using NUnit.Framework;

namespace Infra.Hash.IntegrationTest.Algorithm;

public class Sha384Tests
{
    private readonly IHashAlgorithm _hasher;

    public Sha384Tests()
    {
        var startup = new Startup();

        var hashFactory = startup.GetService<IHashFactory>();

        _hasher = hashFactory.Create(new HashOptions
        {
            Type = HashType.Sha384
        });
    }

    [Test]
    public void HashTextSuccess()
    {
        const string text = "test";

        var hashedText = _hasher.Hash(text);

        Assert.That(hashedText, Is.Not.EqualTo(text));
    }

    [Test]
    public void HashBytesSuccess()
    {
        var bytes = Encoding.UTF8.GetBytes("test");

        var hashedBytes = _hasher.Hash(bytes);

        Assert.That(hashedBytes, Is.Not.Length.EqualTo(bytes.Length));
    }

    [Test]
    public void VerifyHashedTextSuccess()
    {
        const string text = "test";

        var hashedText = _hasher.Hash(text);

        Assert.That(_hasher.Verify(text, hashedText), Is.True);
    }

    [Test]
    public void VerifyHashedBytesSuccess()
    {
        var bytes = Encoding.UTF8.GetBytes("test");

        var hashedBytes = _hasher.Hash(bytes);

        Assert.That(_hasher.Verify(bytes, hashedBytes), Is.True);
    }
}
