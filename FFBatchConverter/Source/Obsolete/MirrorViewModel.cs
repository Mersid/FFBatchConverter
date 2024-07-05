using ReactiveUI;

namespace FFBatchConverter;

[Obsolete("Testing class to experiment with ReactiveUI. We should probably remove this later.")]
public class MirrorViewModel : ReactiveObject
{
    private string _text = "Hello world!";
    public string Text
    {
        get => _text;
        set
        {
            this.RaiseAndSetIfChanged(ref _text, value);

            if (value.Length > 18)
                this.RaiseAndSetIfChanged(ref _text, "Ayy!");
        }
    }

    public MirrorViewModel()
    {

    }
}