using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Escritura : MonoBehaviour
{
    public Text wordOutput = null; //salida de texto
    [SerializeField] private AudioSource audioNivel;

    private const int MAX_ERRORES_SEGUIDOS = 20;

    private string oracionRestante = string.Empty; //Oraci�n que no ha sido escrita
    private string oracionPasada = string.Empty; //Oracion que ya fue escrita que queremos mostrar en la pantalla

    private int aciertos;
    private int errores;
    private int erroresSeguidos;
    private int tiempo;
    private bool errorActual;
    private bool jugando;
    private bool esAudio;
    private int numeroNivel;

    private int[] lineaImagen = new int[3];

    private string[] colorLetra = new string[3]; 
    private string[] texto = new string[30]; //Guardamos todas las lineas de texto que tenga el archivo
    private int numLineaActual = 0; //contador de la linea que estamos mostrando en pantalla actualmente
    private int totalLineas = 0; //Total de lineas leidas del archivo
     
    private bool activo; //Bandera para no poder escribir mientras se cambia el texto

    public Configuracion configuracion; //Objeto de configuracion para poder obtener los valores del texto y volumen
    public Pausa controlPaneles;
    public ReportePostPartida reporte;
    public Image imagenFondo;
    private Sprite[] Ilustraciones = new Sprite[3];
    private AudioClip[] audiosTexto = new AudioClip[30];

    public int nivelAJugar;

    void Start()
    {
        //leemos de configuracion el color y el tama�o del texto
        colorLetra = configuracion.getColorLetra();
        wordOutput.fontSize = configuracion.getTamanioLetra();

        erroresSeguidos = 0;

        //Cargamos las ilustraciones que se van a mostrar
        //Ilustraciones[0] = Resources.Load(nivelAJugar + "/1" , typeof(Sprite)) as Sprite;
        Ilustraciones[0] = Resources.Load<Sprite>("Ilustraciones/" + nivelAJugar + "/1");
        Ilustraciones[1] = Resources.Load<Sprite>("Ilustraciones/" + nivelAJugar + "/2");
        Ilustraciones[2] = Resources.Load<Sprite>("Ilustraciones/" + nivelAJugar + "/3");

        //esAudio = PlayerPrefs.GetInt("varianteAudio"+ PlayerPrefs.GetInt("nivelActual")) == 1 ? true : false;
        esAudio = false;

        activo = false;

        InvokeRepeating("Cronometro", 0f, 1f);//inicia el conteo del tiempo, lo vamos a cambiar para iniciarlo cuando se quite la pantalla de tutorial

        //Asignamos en que linea del archivo deber�a cambiarse el dibujo de fondo, valores hardcodeados por ahora
        lineaImagen[0] = 0;
        lineaImagen[1] = 999;
        lineaImagen[2] = 999;

        //Leo todas las lineas del archivo y las almaceno en un arreglo
        Debug.Log(PlayerPrefs.GetInt("noNivel"));
        int contadorLineas = 1;
        foreach (string line in System.IO.File.ReadLines(@"Assets/Textos/" + nivelAJugar + ".txt")) //PlayerPrefs.GetInt("nivelActual")
        {
            if(line == "\\") //Leemos si la l�nea tiene solamente la diagonal invertida, en cuyo caso en esa l�nea se cambia la imagen
            {
                lineaImagen[contadorLineas] = totalLineas;
                contadorLineas++;
            }
            else //Si no es una linea normal de texto que se debe escribir
            {
                texto[totalLineas] = line;
                totalLineas++;
            }
        }

        for(int i=0; i<contadorLineas; i++)
        {
            audiosTexto[i] = Resources.Load<AudioClip>("Audios/" + nivelAJugar + "/" + (i+1));
        }

        imagenFondo.sprite = Ilustraciones[0];
        SetPalabraActual();
    }
    private void SetPalabraActual()
    {
        //Aqui vemos si ya lleg� a la ultima liena y  si no le actualizamos el texto y controlamos si puede o no escribir

        if(numLineaActual == totalLineas)
        {
            Debug.Log("Ya acabaste al chile");
            jugando = false;
            activo = false;
            Debug.Log("Aciertos: " + aciertos);
            Debug.Log("Errores: " + errores);
            Debug.Log("Tiempo: " + tiempo);

            reporte.GenerarReporte();
            controlPaneles.GanarNivel();
        }
        else if(numLineaActual == 0)
        {
            ActualizarMensaje();
        }
        else
        {
            activo = false;
            StartCoroutine(Esperar());
        }

        if (numLineaActual == lineaImagen[1])
        {
            //Aqu� cambiamos la imagen de fondo por el segundo dibujo, el primer dibujo lo ponemos desde el start
            Debug.Log("Imagen: Las casas de paja y madera construidas, mientras dos cerditos juegan y el tercero sigue construyendo la casa de ladrillos.");
            imagenFondo.sprite = Ilustraciones[1];
        }
        if (numLineaActual == lineaImagen[2])
        {
            //Aqu� cambiamos la imagen de fondo por el tercer dibujo
            imagenFondo.sprite = Ilustraciones[2];
            Debug.Log("Imagen: Las casas de paja y madera destruidas, mientras que los tres festejan afuera de la casa de ladrillo, con el lobo desmayado.");
        }
    }

    private void SetOracionRestante(string palabraActualizada, string palabraAntigua)
    {
        //Aqu� actualizamos lo que se muestra en pantalla
        oracionRestante =  palabraActualizada;
        string palabraMostrar = "<color=\"" + colorLetra[0] + "\">" + palabraActualizada + "</color>";
        if (!esAudio)
        {
            wordOutput.text = palabraAntigua + "|" + palabraMostrar;
        }
        else
        {
            wordOutput.text = palabraAntigua;

        }
    }

    private void Update()
    {
        //Leemos los valores de configuraci�n para detectar cambios
        wordOutput.fontSize = configuracion.getTamanioLetra();
        colorLetra = configuracion.getColorLetra();

        //Capturamos que presione las teclas y verificamos que si puede escribir
        if (Input.anyKeyDown && activo)
        {
            string keysPressed = Input.inputString;
            if (keysPressed.Length == 1)
            {
                CompararCaracter(keysPressed);
            }
        }
    }

    private void CompararCaracter(string typedLetter)
    {
        if (oracionRestante.IndexOf(typedLetter) == 0) //Buscamos la letra ingresada en la posicion 0 de la cadena que debemos escribir
        {
            errorActual = false;
            aciertos++;
        }
        else
        {
            errorActual = true;
            errores++;
        }
        QuitarLetra(); //Quitamos la letra de la cadena independientemente de si fue acierto o error
        if (oracionRestante.Length == 0){ //Verificamos que no haya terminado de escribir la oracion y si lo hizo la cambiamos
            SetPalabraActual();
        }
            
    }

    private void QuitarLetra()
    {
        if (errorActual) //Comprobamos si se equivoc� en la ultima letra para cambiar el color del texto
        {
            erroresSeguidos++;
            oracionPasada += "<color=\"" + colorLetra[1] + "\">" + oracionRestante.Substring(0, 1) + "</color>";
            //"<color=\"" + colorLetra[0] + "\">" + palabraActualizada + "</color>";
        }
        else
        {
            erroresSeguidos = 0;
            oracionPasada += "<color=\"" + colorLetra[2] + "\">" + oracionRestante.Substring(0, 1) + "</color>";
        }

        //Aqu� habr�a una funci�n para reiniciar el nivel si se superan los errores seguidos, ahorita nomas le decimos que perdi�
        if (erroresSeguidos > MAX_ERRORES_SEGUIDOS)
        {
            Debug.Log("Perdiste al chile");
            activo = false;
            jugando = false;
            controlPaneles.PerderNivel();
        }

        string newString = oracionRestante.Remove(0, 1); //Quitamos la ultima letra
        SetOracionRestante(newString, oracionPasada); //Actualizamos el texto en pantalla
    }

    IEnumerator Esperar()
    {   //Esperamos para que se alcance a ver si la �ltima letra de una oraci�n se escribi� bien o mal.
        yield return new WaitForSeconds(.3f);
        ActualizarMensaje();
        activo = true;
    }

    IEnumerator ActivarTeclado()
    {
        yield return new WaitForSeconds(.5f);
        activo = true;
    }

    private void ActualizarMensaje()
    {
        SetOracionRestante(texto[numLineaActual], "");
        oracionPasada = "";
        numLineaActual++;
        if(esAudio)
            audioNivel.PlayOneShot(audiosTexto[numLineaActual - 1]);
    }
    void Cronometro()
    {
        if(jugando)
            tiempo++;
    }

    public void iniciarTiempo()
    {
        jugando = true;
        StartCoroutine("ActivarTeclado");
    }

    public void detenerTiempo()
    {
        jugando = false;
        activo = false;
    }

    public int getTiempo()
    {
        return tiempo;
    }

    public int getAciertos()
    {
        return aciertos;
    }
}
