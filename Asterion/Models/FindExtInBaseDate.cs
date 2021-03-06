﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace Asterion.Models
{
    class FindExtInBaseDate
    {
        public List<DescriptionExtension> descriptExtList= null;
        public DescriptionExtension descriptExt = null;
        string tagetExt;

        public FindExtInBaseDate( string tagetExt )
        {            
            descriptExtList = new List<DescriptionExtension>();
            this.tagetExt = tagetExt;
        }
        /// <summary>
        /// Главная функция поиска по файлам
        /// </summary>
        /// 
        public void FindIn_TXT()
        {
            try
            {
                FindIn_TXT_Start();
            }catch(Exception e ) { MessageBox.Show( e.Message ); }
        }
        public void FindIn_TXT_Start()
        {
            descriptExtList.Clear();
            tagetExt = "." + tagetExt;
            DirectoryInfo textDirectory = new DirectoryInfo( "text" );
            descriptExt = new DescriptionExtension();
            
            foreach( var fileTXT in textDirectory.GetFiles() )
            {
                bool isFind = false;
                descriptExt = new DescriptionExtension();
                descriptExt.Category = Path.GetFileNameWithoutExtension( fileTXT.Name );
                StreamReader reader = File.OpenText( fileTXT.FullName );
                try
                {
                    while( !reader.EndOfStream )
                    {
                        string currentLine = reader.ReadLine();
                        if( isEqualyExt( currentLine ) )
                        {
                            FillingDescriptions( currentLine );
                            descriptExtList.Add( descriptExt );
                            isFind = true;
                            break;
                        }
                    }
                } catch(Exception e) { MessageBox.Show( e.Message ); } finally { reader.Close(); }
                if( isFind )
                {                    
                    continue;
                }
                descriptExt.Category = "Не найдено совпадений";
                descriptExt.Description = "";
            }
        }

        private void FillingDescriptions( string currentLine )
        {
            string paternExt = @"\s\S+";
            //@"\s\w+";

            var regex = new Regex( paternExt );
            MatchCollection findable = regex.Matches( currentLine );
            string sd = regex.Match( currentLine ).Result("${description}");
            foreach( var item in findable )
            {
                string[] s = item.ToString().Split( '\t' );
                if( s.Length > 1 )
                    descriptExt.Description += s[1];
                else
                    descriptExt.Description += s[0];
            }
        }

        /// <summary>
        /// Ищет в строке расширение
        /// </summary>
        /// <param name="currentLine"> строка в которой ещем расширение</param>
        /// <returns></returns>
        bool isEqualyExt( string currentLine )
        {
            string paternExt = @"^\.\S+";
            var regex = new Regex( paternExt );
            Match findable = regex.Match( currentLine );
            return findable.Value == tagetExt;
        }
    }

    class DescriptionExtension
    {
        string category;
        string description;
        //string fullDescription;

        public string Category
        {
            get
            {
                return category;
            }

            set
            {
                category = value;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }
    }
}
// Регулярные выражения.

/*
  
  МЕТАСИМВОЛЫ - это символы для составления Шаблона поиска.
       
  \d    Определяет символы цифр. 
  \D 	Определяет любой символ, который не является цифрой. 
  \w 	Определяет любой символ цифры, буквы или подчеркивания. 
  \W    Определяет любой символ, который не является цифрой, буквой или подчеркиванием. 
  \s 	Определяет любой непечатный символ, включая пробел. 
  \S 	Определяет любой символ, кроме символов табуляции, новой строки и возврата каретки.
   .    Определяет любой символ кроме символа новой строки. 
  \.    Определяет символ точки.
 
 КВАНТИФИКАТОРЫ - это символы которые определяют, где и сколько раз необходимое вхождение символов может встречаться.
 
  ^ - c начала строки. 
  $ - с конца строки. 
  + - одно и более вхождений подшаблона в строке.  
   Квантификатор * значит, что вхождение 0 и более раз...

    Возможно указать необходимые символы между скобок [].
    Строка может состоять только из символов - [qwerty]. Например:  qqq, yyqyqyyyq, eeer ...

    | - символ для указания вариантов шаблона (ИЛИ). 
 */
