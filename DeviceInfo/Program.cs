using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenHardwareMonitor.Hardware;

/*
                                                                                   {zü—                                                               
 zzzzííÏÏzÏÏüzzüíüÏÇ—       zÆ    ÆüíÏzÆí   Æ6ííÅÅ   GÆ6   ÆÆ      Ï6üüÆÇ      ÇÆÆÆ    íÆÆÆÆ —    6ÆÆÆÆ   ÅÆ{  6ÆÆ6     6Æüízzíüü666ü66ÏÏíízzzzzÏÏüüí 
 íü6ü66üÏüÏzzzzÞí6{Ç{       zÆ    ÆÇíÏ Æí   ÆüíÏÆÅ   üÆ    ÆÆ      {ÅÏ{ÆÏ      GÆz  íÆ   —ÆÆ    í   üÆ›   GÆ    ÇÆz      Æüzzzí6666üÏÏÏÏüÏÏÏzzííízíí— 
 ›ÏÏ6666üüüzzííÏ6üüGÅÆÅ   ÆÆÆÆ   üÆÆÆÆÏÆ6   ÆÏ{íGG   ÏÆ{   ÆÆ   gÆÆÅÇí{ÆÇ   ÆÆÆÇÆ   ÆÆÏ   ÆÆ   ÆÆ6  —ÆÏ   ÆÆ—   ÆÆ   Å—  ÆíÏüÏzÏÏüÏüÏÏzÏÏÏÏÏzÏzÏÏÏÏü{ 
 ü6Ïz{zÏÏzÏzzzííííí{—Æü   gÆ{Æ       —ÆÆü   Æü{6ÆÆ   ÆÆ—   ÆÆ   zg—GüzíÆÞ   zÆz—Æ—  ÇÆ    ÆÆ    Ç  ÆÆÆz   {Ç   ›Æg   Æ›  Æ—íÏÏü6ÏüüüÏÏzzízzÏÏÏüÏüü66z 
 ÇÇÇÇ6ÏüzÏÏzÏÏzÏüÏÏzzÆü   GÆ›Æ    Æ    Æü   ÆÇzÇÇü   ÏÆü   ÆÆ      z6z—ÆÇ   ÏÆ{zÆ—  ÏÆ    ÆÆ       ÆÆÆ6         ÆÅ   Æ›  íÆüüíízzÏüüzzÏÏzÏüÏzÏÏzzÏÏÏz 
 z6Ç6ÇÏÏüüÏzÏÏzzzzzzÏÆ6   GÆ›Æ   —Æ6   ÆÞ   Æ6ííüÆz        ÆÆ   GÆÅgüí{ÆÞ   ÏÆííÆ›  ÏÆ›   ÆÆ   ÆÆz   ÆG   ÆÆ—   ÆG  —Æ—   ÆÏ{íí{zÏzzíííÏzzzzí{ííízzz— 
 zÇ6Ïz{íÏÏÏzzzzzíz{—íGü   ÞÅ Æ   zÆÇ   Æ6   ÆüÏGzÞÆÆÆÆÆÆ   ÆÆ   GÆÞÅÞí{ÆÞ   ÏÆ{—Æ   ÆÆÏ   ÆÆ   ÆÆü   ÆÇ   ÆÆ   —Æ         Æz—ÏÏííízzzí{z{züzzííízzzz› 
 {Ïü66í{zzíííííízÏz{{Gz   üG{Æ         ÆÇ   Æüíü{—›{— ü    ÆÆ      ›Gz—Æü   {ÆízÆí   z   ÏÆÆ        —Æü   ÆÆ   üÆ   ÏÅG   ›Æ{z{›—íÏí{—ízí{ÏzzzÏüÏÏÏÏ{ 
 íÏ6GGzíízzííí{íízzííÇÏ——›ÇÏíÆ›Ïüí—í{ÅÆÆÇ Ï ÆüüüÏÏÏÏzíÅÆÏÏ ÆÆ üzü6 6Æ6zÅÆ{GzGÇzígÆÆÞü  ÆÆÆÇÆ í{zzüÆÆÆÆÆÏz ÆÆ — ÆÆí› GÆÆ— ›ÞÆíüízzÇ6{—íÏzzí{íííízzzzz› 
 {zzzÏzíÏzÏzzzzzíííÏízüGÆÆgzíÅÇggÞgÆgÇz6GÅÆGÅÏzzzí{íÏÇÞÆÆÆzííÏÆÅÆÆGÆz  —6gG{Þ{ü{  —66ÞGÆG  ÆGÅGÆÆÆ6gÇ—6ÅÆgüÆÆÆÆÆgÅÆÆÇ üÞÆg—› í{ÏüüzíÏ66zzzíízzzz{í{í› 
  {{{íííÏzzzzzííííízííízí{íÏí{—{{{íz{{{í{íÏzzííí{ÏÅÆÆgzíÆÆÆ6 ÆÆÆÅííÆÆÆÆÆÇÅGÆÆÇÅÇÆÆ6ÏÆÆÆÏGgÆgÆÆÆÞ ÆÞÅÆÅíÆíÏ6Æ ÇÇ6ggÞíÆÞz{íí ÏÞ6íÏí{ííÏ66üüÏíííííízízz› 
 —zzzízíízíí{{{zzzízzz{íz{{zÏzzízzzÏÏÏzííÏzízÏÏzÏÅÆ        ÆGÆ       ÏÆÞ   Æ6   6Æ   Æ›       ÏÆÇz     Æ6Ï6Þ        6ÆÇüGGgGÞ6íí{{{ízÏzÏüzíízzzízzÏü{ 
 ›ííízíízíz{{íííííízzíí{{{{{{{zzííízzzíízzí{{íízzÆ6  {ÆÆ   ÆÆ   zÆÆ   ÆÆ   ÆÆ   ÆÆ   Æ6   ÆÆ   ÅÆ   {   ÆíÞÆ   ÆÆ   gÆGgÞí——{zzzÞGü{{í{››››{íÏí——{Ïüí 
 —íízzíízzízízzíí{{zí{Ï66ÇÏzÏÏ66ÏííííízÏÏÏÏÏízz{ÏGü  —ÆÏ   ÆÆ   —ÆÅ   ÆÆ   ÆÆ  ›ÆÆ   Æg   ÆÆ   ÆÆ  íÆ—  Æ—6Æ   ÆÆ   ÆÆüÏ —í6GÞÇÞGüí——zÏzzzÏÏÏüüíízíÏ{ 
 ›íííí{íííízzzÏí{{›zízü6üüzÏzzüzÏzízzzz{{——ízzí{zÆÞ   ÆÆÆÆÆÆÆ    ÆG   üÆÆÆ         ÆÆÆ{ —      ÆÆ  zÆÏ  ÆzGÆ   ÆÆ   ÆÆ —{züüÏzzííííÞÅÞ6666ÇüÏíííí{{z{ 
  ííííííííízzzüí{{—{ízzzzzzzzzí{{—í{{zzÏÏüüÏÏzzí—ÅG   ÆÆíG—ÆÆ    ÆÅ   ÆÆí  ÆG   ÆÆ  üÆÞ   gÞGÆÆÆÇ  ÞÆÞ  ÞÞÞÆ   ÆÆ   ÆÇ {—í6üÏÏüü6ÞggGz ›{{í{——{—í{——› 
 ›íízízzÏízzíí{{{zzüÏzzííí{—› ››————{ííízÏÏzízÏÏzÅ6  íÆÆ   ÆÆ    ÆÆ   ÆÞ   ÆÆ   ÆÆ   Æz   ÆÞ66{ÆÏ        ÆÆÆ   ÆÆ   Ægz66ÞGÞííí—     ›íÏÏzííí{{{{{{í  
  íííí{{í{íí{{{í{{{íííííí{{ííízíÏzüüüüÏÏÏÏzízzz{{ÅÞ   Æ6   ÆGG   ÆG   ÆÆ   ÆG   gÆ  —ÆÏ   Æüí{—Æ›  ÏÆü   ÆÞ{   ÆÆ   ÆÅüüü6üz—› ízÏüÏzííízzíííí{{{{íí  
 —íííííííííí{íííí{{{{{{{ííí{{íí{{—íííÏÏÏzí{íízÏ{zÞÆg—    ÞÆÆ ÆÏ     GÆÆG   Æí   ÆÆ   Æ›   ÆÏííÏ6    ÆÆ        GÆG   Ç6›››  {züüüÏü6ÇÇÇÇÏz{—{í—íí{{{z  
 —ízzízzzzzzííííí{{{{{íí{{ííízzí{íízzíííííízÏzízí  ÏÅÆÆÆÆÞ í  gÆÆÆÆÆÞ  ÇÆÆGíÇÆÆÆÇÏGÅüÆÞÞÆÆgÏíz6ÆGÆÆG››üÆÆÆÆÆÆÆÇ6ÇÆÆÆÆü ›{——{ÏÏzí{ízzíízíí{——{›{ííí{{  
 ›zzíííízíí{{{íííííí{—{{—{{{—{{íí{{—›{{{í{{í{{{{íí{{››    —zí››   ›—›íz{——{{—›››› ›››{—›—Ï{—{››› ›—{ííí›               í{züüüz{——›     ›—{{{í—{{{{í{  
  í{{{{{{{{{í{íízízzzíí{{{{{{ííí{í{{—íí{í{————{{{{{{{íí{{íí—{{{í{{í{{—›———{{í{{{{í{{{í{{{Ï—{í{zÏÏzí{{{í——ízzí{—{{{ízz——ízÏÏÏÏ{—zÏzzíí{›{{í{íí{——{{í{  
  {íí{í{{{{{{—{{zzzí{{íízzíí{{í{íí{{{ííí{{———{{{{{{—íÏí{——{ííí{íí{íízí{{ííÏ{{—{íííí{—{íííízzzí—{{{{ízííí{í—›—zz{{{{{z{—{züÏzí—{züüÏÏÏ{›——{{{—{———{{í  
  ízzzzí{{í{íííí{íííííí{ííííí{zzíí{{{{——{{{{{{{í{{{{—›{{{{{íí—›{z———›ííí››—›—›——{íííízzzí{{íííízíízííízzíízzí{—{{{››íí{{zzÏ—› í6ü6Ç6Ïí››{—{—{{——{—íz› 
  í{ —ízzí— {{í{{{{{{{{—{{{{{—{{{íííí{{{{—{{{{{{——{———{{í—————íz—          ›—{›    —››   ›{{——————{——————————›————{{——›—{{——›— ›í—›—ííííí{{{{{{—{——›  
  ——›————{——{íz{{{{{{{íííí{{{{{›—{zízí{{{{{{——{—————{{{——{{——         ›››     ›í{—    ———{{———————{{{{{{{{{—í{›—{—{—{—{íí{—{{—››—{{íz›—{{{{{í{{———{—  
  {í{—›———{ízíí{{{{í{{{íííí—{{{—{{íííííí{{{—{—›—›{{—›           ÏgÆÅÆÅÅÅÅÞ6ÏíÏÇGÞ——ÞÆÅzí›—{—{{——————————{—{—{——{{—{—————{{———{—{í{{—›——{{{{—{{{—{{{—  
  zzí{—{{{íí{{{{{{í{{ííííí{—{{z{{í{í—{{{——›                 üÆÆÆÆÆÆÆÆÆÅÅÆÆÆÆÆGGGgüÆÆÆG›—{ ——{{{{{{——{—{—{—{———{{{{{{{{{—{—————›{zíz{{———{——{——{—{—{—  
  zzÏzzzzíííííííííí{{{——››—ízízüüÏzí{{—{{—í6Þ6z{——{{üÅÆÆÆÆÆÆÆÆÆgÞÇÏ6ÆgÇÇÞÞÇ6ÇÇüíÏzÆÆÆ{ —í {{{———{{—{{———{{—{íí{{——›———››{{——{{{{{{›—{{———————{{—{———  
  —ízíí{{íííííííííííí{{zííííí{{——{{ííí{{››íÇÆÆÆÆÆÆÆÆÆÅÅÆÆÆÅÅgGÞ666zÏÇÇüÏzííÏÇÇÇí— ÞÆÆ   › ——›————{——{——›—{—{ííííz{{í{{{—{{—{{ízíz{—{{—{íízí—{{{——{{—  
  íízzzíízzízíííííííííííííííí——›—{{íÏí—{zí—›     ››züü6zíz6gÏÏ666ÇÇÇÞGÇÇÞGÞÏüÇGÅÆÞüÞÆÆí{{ ———————{{{—{›—›{{{íííííí{{{{{—í{{{{í{íí——{{{{í{{í{íí{{—{{{  
 {üÏÏÏíí——————{{{——————{{íízí—›{ízÏüüÏízí—›  ›íÇÞÇ6üüüÇ6üzíÏ66Ç666ÇÇÇÏíÏÇÞÏÏzíüÞÞzüÆÆÆÅzí›—{{{{{{{—{{{{{——››››—›››—{{{íííííí{{—íí—{—{——{—————{————{{  
  {——{{{—{{{í{{ÏÏÏüüzízzz{zzzííí{í{{{zÏzí—››  ›zGgÞÇ66ÞÞÇüÏzÇÞÞGgÅÞgGí—{zzzÏüüüÏüíÏÆÆÆ   ———{————{{{————{—{{í{{—zzí{———{{—{{——›{{{{—{——{————————{{{{  
  ——{{íí{í{zííízzzíÏÏüüÏí{zííízzzí{—›   ››—íí›  í66üzzü66Ï{{ízÏGÆÆgü›     —{gg{  ››GÆÆ{  ›——{———{——————{{{——í{{{í{{——›—{í——{{{{—————{{ízzzí{ííí{{{{{  
  {{zííí{{ííí{{{—››{zÏÏzííí{—ízíí{{{—{—í{ííííí—zÇÇüí{í›         züÏ  gÆÆÆ6—6ÆÆÞ   {gÆÆÆ{  ›—{——{{——{——{————{{{{{{›———ííz{{{—{—{{—{{{———ízzíííí{›—›——  
  {í{{íííííííí{í{—›{——{zzz—›—{{{———›——{———››   zÇÏíüÇGGÞÞÆÆÆÆÆ—  í› 6ÆÆÆÆ   6ÆÆÅ6üÞzíÅÆg   ———›——››——{——›—›{íí{{—›{{{íí{{—{{{{{{{{{{{{—›——————{———{—  
  {{{í{íííí{íí{í{íí{íízzzz{—›{{{{{—› ›››——{{ íüÅÆÅgÅÆÅü   {ÆÆÆÆÆÆÆÆÆÆ6— zÅÆÆgÅgGGgGÏzÅÆÆÞ››————{{——{{{{—{í{{íí{í{———{{——{———{————ízÏí{—{———{————{—{{  
  {íí{ííízííííí—{{{{{———›{—›——{——{{í{{íí{——— ›{üÇüÏ——üÞÅGÞÞí üÇÇggÆgÇÇGGÞÇ6ÆÆÆgüüíízügÆÆí  ——{{{{{{{{—{{——{{{{—›››————{{{{{{{——{{{ízí———{{{{{—————í{  
  ————›—ííííííí{{{{{{›››››——{——›——{ízzzíí{{{—›—zÞÞ6ÏzÞÆÅÆÆÆzzÇüííüGÏ› ›››  íÏÏ6ÇÇü6GgÅÆÆÏ  {———{———{{—{{—›—{——››——{—{—{—{{—{{—{—{{{í{{{—{——{—————{{—  
  {{{—›——í{ííí{íízzzzííí{————{zí———{íííííííí{—  {6ÞÇüüÞÇ6{›—{{ízízüÏí{zÏzzí———Ïz{›—6Ågg—    —{—{—› ——{{——{—{———{———————{———{——{——{{í{——{{{íí{——›{›——  
 ›zzzí{{{{íííí{{{ííízíí{{í{ííííz{{——{›   › ›—{   —zíí{{í{›{ü6666{››{Ïüüz{›———í6ÇÞ6Ï66ÏzÞÆÅ›››{————›——{{———————————{{—{{{{{{í{—{{{———{{—————›———{{{{—  
  {{{{{{íííí{ízzzííí{›{{{{{{íÏ{—————››{———————› —6GÅÅ6üÏ{— ›     ›{í—   —íüüÏzíízü6Ç6ÇÏÆÆÆ—  {››——›——››››—————››——{——————————————————›————{{———{{{—›  
  {{—{{{ízízííííííí{››{íííííí{—{{{———————————› —ÞÆÆgÇÏÏüüÏÏü6ÏÏüí—››{›{Ï6Ç6üÏz{Ïü6ÞÞÇ66ÆÆÆ  ›{›››———{—————{{—————{{{—{—{{{——————{—{————{{{{í{{{——{{—  
  {{{{{íí{{{{—›››››——ííí{———››——›—————————›———  zÞÞü{    ›—{Ç666üÏÏü6üzüÏz{—››››{—6ÞÇüüÆÆÏ  —{———››———{{—{————›——————{——›—{—{{{———{{———{{{{{——{{{———  
  {í{———————{{{{{——{—{{{—{——————››{{————››››—{   —üÞÅÅÞÇÏ{{{—   ›{zíízz› ›{üüÏzzÏÏü6ÇgGÆÆg    {—————›————————————›——{——————{{{{{{{{———{—————{—{{—{{í  
  íí{{—{{{{{———{{{————›————{——{{{{{——{{{{{{———  ›Ç6ÏííízzzízzíÏÇgÅÇí—ÏüüÏÏüÇ6GÞGgGÞ6Ïz› zÆGí  ›—›——››——››————{—————{—{———{{{{{{{——{—{{{——————{{{—{{{  
  íí{——————————{{{——————————{————{{{—{zzí{{—›  zÆÆÅÏ——í666zíÇÞGÞÏ›  {Ïzí{  ›—{{— ›íÇÇÞgÇ6GÆÆ    ›——›——{››———————{{{{{{{{———{—{{{{{í{—{{{—{—{———{{———  
  ííí{{{————ííí{í{{—{—›—————{—{————{—{{{{——— —zÞÅgÞÏzÏzüÏzííÇÇÇ6üüüÅÆGÇÇÇ{ › ›      ›—ÏÞ6ÏüÆÅ   —{—›—›{—————————{{{——{————————{{{{{{{{————{———{{——{—  
  —{ízí{——{í{{{—{——{{{{í{{{—————{—{—{—{{{——— ›ÇÆÆG6üÏü———{íízí{›  {ízÏ6GÞí››—ÇÆÆÆgÇü6üüÞÅÅ6ÆÆÆ  ››——›———————————›———{—›————————{{{{{{{{—{{{{{—{{{{›—  
  {{{{{{{{{{—{—í{{{{{—{{—————{›————{——››››—›  zGÞÇüzzz——{zz{››  ›        ››í{{ízízÏÏÏzüÞÇÇÇÆÆ6   ›———››———————————{——————›››—› ——›››››››››››› ————{—  
  {{{{{í{{{{—{{{{{{{——{{{{——{{—————{{——{————›  zÇÇ6ü6ÇGGÇÏzüüÇGggÇz›   {üÅÆÆÏ   —zzzÏÏzzüüüÏÅÆ{  ››———{———{—{————————›————›———›—{———›————————{{———{—  
  {{{{{í——{{—{{—{{{{—{{{{{——{{——{—{{{{{í{{{—›  í6ÇÞüÏÇÇ6üzízííÏü6ÏíííÏü6Ï— ÏÞÞÞÇÞÇÏzüüü6üz— 6ÆÞ  ››———{›—{{————{—›———————›——›››—{{—›››——›———  —›—›—›  
  {{{{{{{{{{{{{{{{{{—{{{{{—{{{{—{{{í{{{—{{——›  ügGÞ{  ›zÏ6ÞÅÆÅÇÇ6666ü6ÇÇ6{ ügGÇüÇ6ÏzÏÏíüüÇgÇGÆÆ  ››———›——————{íí—›››› ››——————›—í{{——————›——››—›››——  
  {—{{{{{{{í{{{{{{{—{{{{—{———{{{—{———›———››—— —zÇÇÞÏüÇ6üÏÏí—íü6ü› —íí{›—íüÏzÇ6z›››—{í{{üGÆGÇ6ÅÆg—››——{——››››››› ›—›—›——›››——›——{———{——›————{›——›—›››  
  {{{—{{{{{{{{{í{íí————{—{{{—{{{—{›——{í——— zzÇÇÞÇ66Çü6Ïí{——   íÏü—›—{{—  ›z{ÞÞ{—ÏÏí—››{üÇÇÇ66Þgü   ›—›››››››› › —››››››››››———›{——{{——————{————{{{——  
  {{{{{{{{{{{{{íí{{———{{———{——{{{{{——{——{—›zíÞGg6zíí{—í{—{{zGÇÏízí{—›——  ›í››››ÇÆ6ízí— ›ÏÇ6üüüÅÅÏ› —››—›—— ›››› ››››››—›››——›{——————›——————{———{—{{—  
  {{{{{í{{íí{í{íííí{{{{{{{—{{{{{{í{{{{››—{›{›íügGÞ6ÏÏ6ggGÞÇ6ÆÆgü   ›—{—›—{z—{zÏüÏ —zzzz6ÇÞÏíí6ÆÆü  ››—›››››››  ›——›››››››———{————›—————›————{—›—{íí{  
  —{{{————————›››—————{{{{—{—{——›››› ——————  —ÏÞÏz6üüü66{›   ›—zzzízí›—  —{zÞÇ{    ›{üz6ÇgGÏüÆÆÞ   —› ››› ››——›—››››››››››››››››————————————›››———{{  
  ——{{—{——————{{————{——{——{{—{—{———›———————— 6GÆÞÏÏí{{zz————Ïz› ›{—üÇÏüzzzzz6ÞüÏÏüüÏ6üz6GÆg66ÆÆÞ{  ›  ››  ›    ›››››››››››——›› ›››››››››››    ——————  
  —{———{{—{{—{—{{{{{{{{————————{————›————{{—  —ügGGGÇÇGÞÏ›› üü66{íÏgÅgG6üüÏ{íí{——›   ——{ÇÆgÞGÆÅ—  ›—›———{{—————{››››››——›››—›››—›››››                 
  ›—›———{{{{{{————{——{———{—{{{{{——››————{{{í{  {ÞÅGÞGÇÞÇÏ›  zíí{{{ü6ÏÏ6ÇÞÞÞü6Çü—›    {  {ÆÆGÞgü   ››—{—›››››                            —ízÏ6Ç6üÏzz6z 
   ›—›       ››››     ›—{{{—››——{{{{———{› ›     ÏÆÆÅGÇüü6gGÅÅÇÞÞGüíÇÇüÏÏÏzüüÏÇ66ÆÆGGÆÆÆÆÅ66ÇÅÆÆ              ››    ›       —————   —{íz6ÞÞ6üüüÇÞÞÇ66{ 
  ›{zí— ›         —{›——›        ››——›—————{  ÏGÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÞÅÆÆÆÆÆ6ÞgGÞGGÞüzzü6ÞGgGÞÞÇÇ6ÇgÆÆÆÅÞÏü{›{—{{——{íí——  
 zí{{ÏÏÇÇÇGGÞÇÏüüzÏÇüzÏz›   ›{{—›           ÆÆ      {ÆÆz   zÆÞÆÆÆÆÏ   6Æ    Æ    ÅÆG      zgÅ     ÆÏ   ÆÇí—     6ÆGüÏÏí{züüí———z6gÆÆÆÆÆ6—íííííí6GÆÆÆÆÏ
 ÆÆÇííí6z›   ÏGÆÆ{ ›— íÅÆÆÆÅÏízüüüÇÅÆÅz{›{í Æ   ÆÆ—   ÆÞ   ÆÞ    Ï—   ÅÆ    Æ{   gG   ÆÆ    ÆÆ   íÆ›   Æ  —ÇÆÆÆÆÆÆGí{zzzzzí        {ügÆ—  ——       üÆÞ
  ››    ›6Çü  ÞGÞüí    —ÇggÇü6ÏÏÏ6GgÅGÇÅGÇÞGÆ   ÅÆ    ÆG   ÆÆÆÆÆÇÆÇ   GÆ   {ÆÅ   ÆÆ   ÆÆ    ÆÅ   ÅÆí   Ï   {ÆÅÇz     —{{— —ÅÆÆÞÇÇü{   ü{     6Ç6zÇÆÅ{ 
  ——   —ÞÆÆÆÆÆÆÆÆÆÆÆÅgÇ{zííízÏ6üÏzí{ —6ÆÆÆGÅÆ   íÆÞ{í6ÆÏ        ÇÆÅ   ÆÆ         ÆÆ   GÆ    ÆÆ   ›Ç  ÏÆ› ÅÅÆÆÆÆÆg—   —{—— ›{{        ÇÆÆÆÆÆÆÆÆÆÆÆÆÆÏ  
 zÞÞÇüz{í{  —züÇÞ6ÏÇ6GüízzÏzííÏÏÏüüüüí    ügÅ   {ÆÆÆÆÆÆÆ   ÆÆ   —ÆG   ÆÆ    g    ÆÆ   GÆ    ÆÆ       ÆÆÆÆÆÆÆÆÆÅgÆÆÇ      ›        {{ÞÆÆÆÆü›      Ïí   
  ííízü6ÏÏ6Þ6Ï› {› GÆÆÞíÏí›  —zÏÏüüz——zí{  zÅ   ÇÆ{   ÆÅ   ÆÆ    Æ    GÆ    ÆÆ   ÆÆ   ÆÆ    ÆÆ   ÆÆÇ   ÅzÏÞÞÆÆÆÆÆÆÆü        zÆÆgÞÆÆÆÆÆÆÆg         zÏ  
 ›{{—{{züz{{—{zÏzííÇz{{{› ígg{  ›z›  {ÆÆÆ   g   ÆÆÆ   Æ6   ÆÆ    Æ{   6Æ    Æ    ÆÆ   ÆÆ{   ÆÆ   ›Æ{  ›Ï      gÆGÞÆÆgÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÅ{      —z— 
 z66üü66z{{—› ›› ›—ü   —{{GÆÆÆ      ÅÆÆÆÆÆÆÆÆG      ÏÆÆG        ÇÆÏ    Æ    Æ    6Æü       GÆÆ    Æ›   ÆüÞz    ÏggÞÆÆÆÆÆÆÆÆÆÏ—    ÞÆÆÇ      —ííí›     
       ›   —{ííííí6ÞÏíí{{— {6ÇÇÅGGÆÆÞ›       ÆÅÆÆÅÆÆÆ{GÆÆÆÆÆÆÆÆÆÆGÇÆÆÆÞÅ{—ÆÆÆÆGgGgügÆÆÆÆÆÆÆÆÆÆÇgG6ÆÆÆÆÆÆgÞÞÆÆÆGgÆÆgÆÆÆÆÆÆÆÆü       —Þ     ›    {íÆÆÆÆ 
                               ›{—›             —ü—                       Ggí         6ÅÞzü6z                   —  ›                Ï                 
 */

namespace DeviceInfo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var temperature = new Temperature())
            {
                Console.WriteLine("Инициализация драйверов AMD Ryzen. Пожалуйста, подождите...");

                // Даем время на инициализацию (10-15 секунд)
                for (int i = 0; i < 15; i++)
                {
                    Console.WriteLine($"Ожидание инициализации... {i + 1}/15 сек");
                    Thread.Sleep(1000);

                    // Проверяем, появились ли данные о температуре
                    var temps = temperature.GetTemperaturesInCelsius();
                    if (temps.Count > 0)
                    {
                        Console.WriteLine("Данные о температуре успешно получены!");
                        break;
                    }
                }

                Console.WriteLine("\nТекущие показания температуры:");
                Console.WriteLine("-------------------");

                while (true)
                {
                    var temps = temperature.GetTemperaturesInCelsius();

                    if (temps.Count == 0)
                    {
                        Console.WriteLine("Данные о температуре недоступны. Продолжаем попытки...");
                    }
                    else
                    {
                        foreach (var temp in temps)
                        {
                            Console.WriteLine($"{temp.Key}: {temp.Value}°C");
                        }
                    }

                    Console.WriteLine("-------------------");
                    Thread.Sleep(1000);
                }
            }
        }
    }
}

public class Temperature : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _updateVisitor;
    private readonly object _lock = new object();
    private bool _isInitialized = false;
    private int _initializationDelay = 0;
    private const int REQUIRED_INITIALIZATION_CYCLES = 10; // Требуется больше циклов для Ryzen

    public Temperature()
    {
        _updateVisitor = new UpdateVisitor();
        _computer = new Computer
        {
            CPUEnabled = true,
            GPUEnabled = true,
            RAMEnabled = true,
            MainboardEnabled = true,
            FanControllerEnabled = true,
            HDDEnabled = true
        };

        _computer.HardwareAdded += DeviceAdded;
        _computer.Open();

        // Важно: не используем Thread.Sleep здесь, так как это блокирует инициализацию
        _isInitialized = true;
    }

    private void UpdateAll()
    {
        lock (_lock)
        {
            _computer.Accept(_updateVisitor);
        }

        // Увеличиваем счетчик инициализации
        if (_initializationDelay < REQUIRED_INITIALIZATION_CYCLES)
            _initializationDelay++;
    }

    public IReadOnlyDictionary<string, float> GetTemperaturesInCelsius()
    {
        if (!_isInitialized) return new Dictionary<string, float>();

        // Делаем несколько обновлений для инициализации данных Ryzen
        for (int i = 0; i < 5; i++)
        {
            UpdateAll();
            Thread.Sleep(200);
        }

        var temperatures = new Dictionary<string, float>();

        lock (_lock)
        {
            foreach (var hardware in _computer.Hardware)
            {
                // Ищем только CPU
                if (hardware.HardwareType == HardwareType.CPU &&
                    hardware.Name.Contains("Ryzen") &&
                    hardware.Name.Contains("AMD"))
                {
                    // Ищем сенсоры с именами Tdie или Tctl
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.Name.Equals("Tdie", StringComparison.OrdinalIgnoreCase) ||
                            sensor.Name.Equals("Tctl", StringComparison.OrdinalIgnoreCase) ||
                            sensor.Name.Equals("SoC", StringComparison.OrdinalIgnoreCase) ||
                            sensor.Name.Equals("Package", StringComparison.OrdinalIgnoreCase))
                        {
                            if (sensor.Value.HasValue && sensor.Value > 0)
                            {
                                temperatures[$"{hardware.Name} - {sensor.Name}"] = sensor.Value.Value;
                            }
                        }
                    }

                    // Если не нашли специальные сенсоры, проверяем подкомпоненты
                    foreach (var subHardware in hardware.SubHardware)
                    {
                        foreach (var sensor in subHardware.Sensors)
                        {
                            if (sensor.Name.Equals("Tdie", StringComparison.OrdinalIgnoreCase) ||
                                sensor.Name.Equals("Tctl", StringComparison.OrdinalIgnoreCase) ||
                                sensor.Name.Equals("SoC", StringComparison.OrdinalIgnoreCase) ||
                                sensor.Name.Equals("Package", StringComparison.OrdinalIgnoreCase))
                            {
                                if (sensor.Value.HasValue && sensor.Value > 0)
                                {
                                    temperatures[$"{subHardware.Name} - {sensor.Name}"] = sensor.Value.Value;
                                }
                            }
                        }
                    }
                }
            }
        }

        return temperatures;
    }

    private void ProcessHardware(IHardware hardware, Dictionary<string, float> result)
    {
        // Специальная обработка для AMD Ryzen
        if (hardware.HardwareType == HardwareType.CPU &&
            hardware.Name.Contains("Ryzen") &&
            hardware.Name.Contains("AMD"))
        {
            ProcessRyzenSensors(hardware, result);
        }
        else
        {
            ProcessStandardSensors(hardware, result);
        }

        // Обрабатываем подкомпоненты
        foreach (var subHardware in hardware.SubHardware)
        {
            ProcessHardware(subHardware, result);
        }
    }

    private void ProcessRyzenSensors(IHardware hardware, Dictionary<string, float> result)
    {
        // Для Ryzen 4000/5000 температура часто находится в сенсорах с именами "Tdie" или "Tctl"
        // Но сначала нужно дать время драйверам инициализироваться

        foreach (var sensor in hardware.Sensors)
        {
            // Проверяем специфические имена сенсоров для Ryzen
            if (sensor.Name.Contains("Tdie") ||
                sensor.Name.Contains("Tctl") ||
                sensor.Name.Contains("Package") ||
                sensor.Name.Contains("SoC"))
            {
                if (sensor.Value.HasValue)
                {
                    result[$"{hardware.Name} - {sensor.Name}"] = sensor.Value.Value;
                }
            }
            // Иногда температура может быть в сенсорах Load после инициализации
            else if ((sensor.SensorType == SensorType.Load ||
                     sensor.SensorType == SensorType.Temperature) &&
                     sensor.Value.HasValue &&
                     sensor.Value > 0) // Для Ryzen значения температуры > 0
            {
                result[$"{hardware.Name} - {sensor.Name}"] = sensor.Value.Value;
            }
        }

        // Если не найдено сенсоров температуры, попробуем найти их в других местах
        if (result.Count == 0)
        {
            foreach (var sensor in hardware.Sensors)
            {
                // Для некоторых версий драйверов температура может быть в сенсорах с другими именами
                if (sensor.Name.Contains("Temp") ||
                    sensor.Name.Contains("Temperature") ||
                    sensor.Name.Contains("Core") ||
                    sensor.Name.Contains("CPU"))
                {
                    if (sensor.Value.HasValue && sensor.Value > 0)
                    {
                        result[$"{hardware.Name} - {sensor.Name}"] = sensor.Value.Value;
                    }
                }
            }
        }
    }

    private void ProcessStandardSensors(IHardware hardware, Dictionary<string, float> result)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if ((sensor.SensorType == SensorType.Temperature ||
                 sensor.Name.Contains("Temp") ||
                 sensor.Name.Contains("Temperature")) &&
                sensor.Value.HasValue)
            {
                result[$"{hardware.Name} - {sensor.Name}"] = sensor.Value.Value;
            }
        }
    }

    private void DeviceAdded(IHardware hardware)
    {
        Console.WriteLine($"Hardware added: {hardware.Name} ({hardware.HardwareType})");

        // Для отладки
        LogHardwareDetails(hardware);
    }

    private void LogHardwareDetails(IHardware hardware)
    {
        Console.WriteLine($"Hardware: {hardware.Name}");
        Console.WriteLine($"  Type: {hardware.HardwareType}");
        Console.WriteLine($"  Sensors count: {hardware.Sensors.Length}");
        Console.WriteLine($"  SubHardware count: {hardware.SubHardware.Length}");

        // Логируем все сенсоры
        foreach (var sensor in hardware.Sensors)
        {
            string valueInfo = sensor.Value.HasValue ? sensor.Value.Value.ToString() : "null";
            Console.WriteLine($"  Sensor: {sensor.Name} | Type: {sensor.SensorType} | Value: {valueInfo}");
        }

        // Логируем подкомпоненты
        foreach (var subHardware in hardware.SubHardware)
        {
            Console.WriteLine($"  SubHardware: {subHardware.Name} | Sensors: {subHardware.Sensors.Length}");
            foreach (var sensor in subHardware.Sensors)
            {
                string valueInfo = sensor.Value.HasValue ? sensor.Value.Value.ToString() : "null";
                Console.WriteLine($"    SubSensor: {sensor.Name} | Type: {sensor.SensorType} | Value: {valueInfo}");
            }
        }
    }

    public void Dispose()
    {
        try
        {
            _computer.Close();
        }
        catch
        {
            // Игнорируем ошибки при закрытии
        }
    }
}

// Класс UpdateVisitor, необходимый для правильного обновления данных
internal class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        hardware.Traverse(this);
    }

    public void VisitSensor(ISensor sensor)
    {
        // Ничего не делаем - обновление происходит в hardware.Update()
    }

    public void VisitParameter(IParameter parameter)
    {
        // Ничего не делаем
    }
}