import java.io.BufferedInputStream
import java.io.File
import java.io.PrintWriter
import kotlin.math.sqrt

class LZWBinFa {

    private class Csomopont(
        private val betu: Char = '/'
    ) {
        private var balNulla: Csomopont? = null
        private var jobbEgy: Csomopont? = null

        fun nullasGyermek(): Csomopont? = balNulla

        fun egyesGyermek(): Csomopont? = jobbEgy

        fun ujNullasGyermek(gy: Csomopont) {
            balNulla = gy
        }

        fun ujEgyesGyermek(gy: Csomopont) {
            jobbEgy = gy
        }

        fun getBetu(): Char = betu
    }

    private val gyoker = Csomopont('/')
    private var fa: Csomopont = gyoker

    private var melyseg = 0
    private var atlagosszeg = 0
    private var atlagdb = 0
    private var maxMelyseg = 0

    private var atlag = 0.0
    private var szoras = 0.0
    private var szorasosszeg = 0.0

    fun addBit(b: Char) {
        if (b == '0') {
            if (fa.nullasGyermek() == null) {
                val uj = Csomopont('0')
                fa.ujNullasGyermek(uj)
                fa = gyoker
            } else {
                fa = fa.nullasGyermek()!!
            }
        } else {
            if (fa.egyesGyermek() == null) {
                val uj = Csomopont('1')
                fa.ujEgyesGyermek(uj)
                fa = gyoker
            } else {
                fa = fa.egyesGyermek()!!
            }
        }
    }

    fun kiir(writer: PrintWriter) {
        melyseg = 0
        kiir(gyoker, writer)
    }

    private fun kiir(elem: Csomopont?, writer: PrintWriter) {
        if (elem != null) {
            ++melyseg

            kiir(elem.egyesGyermek(), writer)

            repeat(melyseg) {
                writer.print("---")
            }

            writer.println("${elem.getBetu()}(${melyseg - 1})")

            kiir(elem.nullasGyermek(), writer)

            --melyseg
        }
    }

    fun getMelyseg(): Int {
        melyseg = 0
        maxMelyseg = 0

        rMelyseg(gyoker)

        return maxMelyseg - 1
    }

    private fun rMelyseg(elem: Csomopont?) {
        if (elem != null) {
            ++melyseg

            if (melyseg > maxMelyseg) {
                maxMelyseg = melyseg
            }

            rMelyseg(elem.egyesGyermek())
            rMelyseg(elem.nullasGyermek())

            --melyseg
        }
    }

    fun getAtlag(): Double {
        melyseg = 0
        atlagosszeg = 0
        atlagdb = 0

        rAtlag(gyoker)

        atlag = atlagosszeg.toDouble() / atlagdb

        return atlag
    }

    private fun rAtlag(elem: Csomopont?) {
        if (elem != null) {
            ++melyseg

            rAtlag(elem.egyesGyermek())
            rAtlag(elem.nullasGyermek())

            --melyseg

            if (elem.egyesGyermek() == null &&
                elem.nullasGyermek() == null
            ) {
                ++atlagdb
                atlagosszeg += melyseg
            }
        }
    }

    fun getSzoras(): Double {
        atlag = getAtlag()

        szorasosszeg = 0.0
        melyseg = 0
        atlagdb = 0

        rSzoras(gyoker)

        szoras = if (atlagdb - 1 > 0) {
            sqrt(szorasosszeg / (atlagdb - 1))
        } else {
            sqrt(szorasosszeg)
        }

        return szoras
    }

    private fun rSzoras(elem: Csomopont?) {
        if (elem != null) {
            ++melyseg

            rSzoras(elem.egyesGyermek())
            rSzoras(elem.nullasGyermek())

            --melyseg

            if (elem.egyesGyermek() == null &&
                elem.nullasGyermek() == null
            ) {
                ++atlagdb

                szorasosszeg +=
                    ((melyseg - atlag) * (melyseg - atlag))
            }
        }
    }
}

fun usage() {
    println("Usage: lzwtree in_file -o out_file")
}

fun main(args: Array<String>) {

    if (args.size != 3) {
        usage()
        return
    }

    val inFile = args[0]

    if (args[1] != "-o") {
        usage()
        return
    }

    val outFile = args[2]

    val inputFile = File(inFile)

    if (!inputFile.exists()) {
        println("$inFile nem letezik...")
        usage()
        return
    }

    val binFa = LZWBinFa()

    BufferedInputStream(inputFile.inputStream()).use { reader ->

        // első sor átugrása
        while (true) {
            val b = reader.read()

            if (b == -1 || b == 0x0A) {
                break
            }
        }

        var kommentben = false

        while (true) {

            var b = reader.read()

            if (b == -1) {
                break
            }

            if (b == 0x3E) { // >
                kommentben = true
                continue
            }

            if (b == 0x0A) { // newline
                kommentben = false
                continue
            }

            if (kommentben) {
                continue
            }

            if (b == 0x4E) { // N
                continue
            }

            for (i in 0 until 8) {

                if ((b and 0x80) != 0) {
                    binFa.addBit('1')
                } else {
                    binFa.addBit('0')
                }

                b = (b shl 1) and 0xFF
            }
        }
    }

    PrintWriter(outFile).use { writer ->

        binFa.kiir(writer)

        writer.println("depth = ${binFa.getMelyseg()}")
        writer.println("mean = ${binFa.getAtlag()}")
        writer.println("var = ${binFa.getSzoras()}")
    }
}