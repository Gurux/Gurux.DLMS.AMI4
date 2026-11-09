using Bunit;
using Gurux.DLMS.AMI.Client.Pages.DeviceTemplate;
using Gurux.DLMS.AMI.Shared.DTOs;
using Gurux.UI.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

static BunitContext CreateContext()
{
    var context = new BunitContext();
    context.JSInterop.Mode = JSRuntimeMode.Loose;
    context.Services.AddSingleton<IGXLocalStorage>(new GXLocalStorage(null, null));
    return context;
}

static (IRenderedComponent<CascadingValue<EditContext>> View, EditContext Form) Render(BunitContext context,
    GXAttributeTemplate template, bool disabled = false)
{
    var form = new EditContext(template);
    var view = context.Render<CascadingValue<EditContext>>(parameters => parameters
        .Add(value => value.Value, form)
        .Add(value => value.IsFixed, true)
        .Add(value => value.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenComponent<AttributeExpirationEditor>(0);
            builder.AddAttribute(1, nameof(AttributeExpirationEditor.Template), template);
            builder.AddAttribute(2, nameof(AttributeExpirationEditor.Disabled), disabled);
            builder.CloseComponent();
        })));
    return (view, form);
}

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new Exception(message);
    }
}

using (var context = CreateContext())
{
    var template = new GXAttributeTemplate();
    var (view, form) = Render(context, template);
    Check(string.IsNullOrEmpty(view.Find("input[type=text]").GetAttribute("value")), "A missing expiration must render an empty nullable duration.");
    Check(!form.IsModified(), "Rendering must not mark expiration modified.");
    Check(view.FindComponent<GXInputTimeSpan<TimeSpan?>>() != null, "The editor must use nullable TimeSpan with the shared duration control.");
    view.Find("input[type=text]").Change("02:03:04");
    Check(template.ExpirationTime?.TimeOfDay == new TimeSpan(2, 3, 4), "Editing the duration must update the DTO's compatible clock representation.");
    Check(form.IsModified(new FieldIdentifier(template, nameof(GXAttributeTemplate.ExpirationTime))), "Changes must mark the actual DTO expiration field modified.");
    view.Find("input[type=text]").Change("00:00:00");
    Check(template.ExpirationTime == null, "Zero time must retain the existing read-on-every-request null meaning.");
    view.Find("input[type=text]").Change("01:00:00");
    view.Find("input[type=text]").Change("");
    Check(template.ExpirationTime == null, "Clearing a nullable duration must clear expiration.");
}
Console.WriteLine("PASS nullable TimeSpan editor, duration edits, zero/empty semantics and DTO edit tracking.");

using (var context = CreateContext())
{
    var original = new DateTimeOffset(2024, 5, 10, 1, 2, 3, TimeSpan.FromHours(3));
    var template = new GXAttributeTemplate { ExpirationTime = original };
    var (view, form) = Render(context, template);
    Check(view.Find("input[type=text]").GetAttribute("value") == "01:02:03", "Existing values must retain clock time without timezone conversion.");
    Check(!form.IsModified(), "Loading stored expiration must not modify it.");
    view.Find("input[type=text]").Change("04:05:06");
    Check(template.ExpirationTime?.Date == original.Date && template.ExpirationTime?.Offset == original.Offset,
        "Editing the duration must preserve the existing stored date and offset.");
    Check(template.ExpirationTime?.TimeOfDay == new TimeSpan(4, 5, 6), "New duration must be stored exactly.");
}
Console.WriteLine("PASS stored date, offset and clock-time compatibility.");

using (var context = CreateContext())
{
    var template = new GXAttributeTemplate { ExpirationTime = DateTimeOffset.MaxValue };
    var (view, form) = Render(context, template);
    Check(view.Find("input[type=checkbox]").HasAttribute("checked"), "The static sentinel must select read-once mode.");
    Check(view.Find("input[type=text]").HasAttribute("disabled"), "Static mode must disable duration editing.");
    Check(!form.IsModified(), "Recognizing static mode must not modify the model.");
    view.Find("input[type=checkbox]").Change(false);
    Check(template.ExpirationTime == null && !view.Find("input[type=text]").HasAttribute("disabled"), "Disabling static must clear its sentinel and re-enable duration editing.");
    view.Find("input[type=text]").Change("00:10:00");
    view.Find("input[type=checkbox]").Change(true);
    Check(template.ExpirationTime == DateTimeOffset.MaxValue, "Selecting read-once must store the existing static sentinel.");
}
Console.WriteLine("PASS static sentinel recognition, clearing and selection.");

using (var context = CreateContext())
{
    var value = new DateTimeOffset(2024, 1, 2, 1, 0, 0, TimeSpan.Zero);
    var template = new GXAttributeTemplate { ExpirationTime = value };
    var (view, form) = Render(context, template, disabled: true);
    Check(view.Find("input[type=text]").HasAttribute("disabled") && view.Find("input[type=checkbox]").HasAttribute("disabled"), "Read-only mode must disable both controls.");
    view.Find("input[type=checkbox]").Change(true);
    Check(template.ExpirationTime == value && !form.IsModified(), "A disabled editor must not change the DTO.");
}
Console.WriteLine("PASS disabled editor preserves values and edit state.");

using (var context = CreateContext())
{
    var value = new DateTimeOffset(2024, 1, 2, 1, 0, 0, TimeSpan.Zero);
    var template = new GXAttributeTemplate { ExpirationTime = value };
    var (view, _) = Render(context, template);
    view.Find("input[type=text]").Change("1.01:00:00");
    Check(template.ExpirationTime == value && view.FindAll(".validation-message").Count > 0,
        "The clock-based attribute DTO must reject whole days instead of silently dropping them.");
    view.Find("input[type=text]").Change("01:00:00");
    Check(template.ExpirationTime == value && view.FindAll(".validation-message").Count == 0,
        "Correcting a day duration back to the stored clock value must clear validation.");
    view.Find("input[type=text]").Change("not-a-duration");
    Check(template.ExpirationTime == value && view.FindAll(".validation-message").Count > 0,
        "Invalid input must report validation without changing expiration or crashing the UI.");
    view.Find("input[type=checkbox]").Change(true);
    Check(template.ExpirationTime == DateTimeOffset.MaxValue && view.FindAll(".validation-message").Count == 0,
        "Switching to read-once must clear validation from the disabled duration input.");
}
Console.WriteLine("PASS invalid duration is reported without an unhandled error.");
