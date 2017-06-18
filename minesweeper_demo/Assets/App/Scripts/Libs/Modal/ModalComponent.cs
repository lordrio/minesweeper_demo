using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public interface ModalBase
{
}

[ExecuteInEditMode]
public class ModalComponent : MonoBehaviour
{
    public enum ComponentTypeEnum
    {
        Invalid,
        Button,
        TextInput,
        ToggleGroup,
        Toggle,
        Slider,
        DropDown,
        Text,
        ModalBase,
        WebPage,
    }

    public enum ComponentDissmissEnum
    {
        None,
        Success,
        Cancel,
        Fail,
    }

    // ComponentName
    [SerializeField]
    private string componentName = string.Empty;

    public string ComponentName
    {
        get
        {
            return componentName;
        }

        set
        {
            componentName = value;
        }
    } 

    // ComponentType
    [SerializeField]
    private ComponentTypeEnum componentType = ComponentTypeEnum.Invalid;

    public ComponentTypeEnum ComponentType
    {
        get
        {
            return componentType;
        }

        private set
        {
            componentType = value;
        }
    }

    // ComponentDissmissType
    [SerializeField]
    private ComponentDissmissEnum componentDismiss = ComponentDissmissEnum.None;

    public ComponentDissmissEnum ComponentDismiss
    {
        get
        {
            return componentDismiss;
        }

        set
        {
            componentDismiss = value;
        }
    }

    [SerializeField]
    private Component componentItem = null;
    public T FetchComponent<T>() where T : Component
    {
        if (componentItem == null)
        {
            componentItem = GetComponent<T>();
        }
        return (T)componentItem;
    }

    // Update is called once per frame
    private void Update()
    {
#if UNITY_EDITOR
        if (componentType == ComponentTypeEnum.Invalid)
        {
            System.Type[] toCheck = {
                typeof(Button),
                typeof(InputField),
                typeof(ToggleGroup),
                typeof(Toggle),
                typeof(Slider),
                typeof(Dropdown),
                typeof(Text),
                typeof(ModalBase)};
            // Component勝手に設定する
            Component comp = null;
            for(int i = 0; i < toCheck.Length; ++i)
            {
                if((comp = GetComponent(toCheck[i])) != null)
                {
                    componentType = (ComponentTypeEnum)(++i);
                    componentItem = comp;
                    break;
                }
            }

        }
#endif
    }
}
