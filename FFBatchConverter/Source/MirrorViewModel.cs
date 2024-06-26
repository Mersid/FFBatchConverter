using ReactiveUI;

namespace FFBatchConverter;

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