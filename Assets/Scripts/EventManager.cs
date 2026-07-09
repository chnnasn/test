using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EventManager : LazySingleton<EventManager>
{

    public Action<float> OnAttackedAction;
}
