import java.io.BufferedReader;
import java.io.FileNotFoundException;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;

public class Formatter {

    private ArrayList<String> callgraph;

    public Formatter(ArrayList<String> callgraph){
    
        this.callgraph = callgraph;
    
    }
    
    public ArrayList<String> ProcessCallGraph(){
    
        ArrayList<String> processedString = new ArrayList<String>();
        
        for(int i = 0; i < callgraph.size(); i++){
        
            String temp = callgraph.get(i);
            String firstString = "";
            String secondString = "";
            String thirdString = "";
            String forthString = "";
            
            boolean initFlag = false;
            
            String[] firstArray;
            String[] secondArray;
            
            int begin = 0;
            int mid = 0;
            int end = temp.length();
            
            
            mid = temp.indexOf("||");
            
            
            firstString = temp.substring(begin, mid);
            thirdString = temp.substring(mid + 2, end) ;
            

            firstArray = firstString.split("\\.");
            secondArray = thirdString.split("\\.");
            
            
             //first part
            firstString = firstArray[firstArray.length - 2];
            
            //second part
            secondString = firstArray[firstArray.length - 1];
            
            
            forthString = secondArray[secondArray.length - 1];
            
            //removing parentesis
            if(secondString.contains("(")){
                
                int counter = secondString.indexOf("(");
                secondString = secondString.substring(0, counter);
            
            }
            
            if(forthString.contains("(")){
            
                int counter = forthString.indexOf("(");
                forthString = forthString.substring(0, counter);
            
            }
            
            if(forthString.equals(secondArray[(secondArray.length) - 2])){
                
                initFlag = true;
            }
            
            //preparing for output
            processedString.add(firstString + " " + firstString);
            
            String tempString = "";
            for(int j = 0; j < (secondArray.length - 1); j++){
                
                if(j != secondArray.length - 2){
                    tempString += secondArray[j] + ".";
                }else{
                    tempString += secondArray[j];
                }
            }
            
            //case initFlag true
            if(initFlag){
    
                processedString.add(firstString + ":" + secondString + " " + tempString + ":<init>");
                
            }else{
            
                processedString.add(firstString + ":" + secondString + " " + tempString + ":" + forthString);
            
            }
            
            processedString.add(firstString + " " + tempString);
         
        }
        
        
        return processedString;
    
    }
    
}

