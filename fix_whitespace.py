with open("tests/SalmonEgg.Infrastructure.Tests/Client/AcpClientTests.cs", "r") as f:
    content = f.read()

# Revert my test patch completely because the flakiness is probably due to runner speed.
# And instead of Task.Delay(200), we should probably use a proper synchronization primitive or longer delay
# Wait, actually the format check only checks changed files.
# Let's just restore the file completely. If it was modified before by me, I will undo it.
